// DynamicJobScheduler.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SilkierQuartz;
using SilkierQuartz.HostedService;

using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class DynamicJobScheduler : IJob
  {
    private readonly NpgSqlContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CheckEngineEvaluation> _logger;
    private static readonly JobConfiguration _jobConfig = new JobConfiguration
    {
      JobName = "Dynamic Job Scheduler",
      JobDescription = "Operates on an interval to determine if any new jobs need to be executed, scheduled, or rescheduled",
      JobGroup = Constants.QuartzGroups.QUARTZ,
      JobType = "DynamicJobScheduler",
      TriggerName = "Check job queue every minute",
      TriggerDescription = "Operate every minute to determine if there are any new jobs to be scheduled or rescheduled",
      CronExpression = "0 0/1 * * * ?",
      IsActive = true,
    };

    public DynamicJobScheduler(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      _configuration = configuration;
    }

    private static JobKey CreateJobKey(JobConfiguration jobConfig)
    {
      return new JobKey(jobConfig.JobName, jobConfig.JobGroup);
    }

    private static TriggerBuilder CreateTriggerBuilder(JobConfiguration jobConfig)
    {
      return TriggerBuilder.Create()
        .WithIdentity(CreateTriggerKey(jobConfig))
        .WithDescription(jobConfig.TriggerDescription)
        .WithSchedule(CronScheduleBuilder.CronSchedule(jobConfig.CronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }

    private static TriggerKey CreateTriggerKey(JobConfiguration jobConfig)
    {
      return new TriggerKey(jobConfig.TriggerName, jobConfig.JobGroup);
    }

    private static Type GetJobType(JobConfiguration jobConfig)
    {
      var jobType = Type.GetType($"Epa.Camd.Quartz.Scheduler.Jobs.{jobConfig.JobType}");
      if (jobType == null)
      {
        throw new Exception($"Job type {jobConfig.JobType} not found");
      }
      return jobType;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      Console.WriteLine("Starting Dynamic Job Scheduler");

      try
      {
        var jobsToSchedule = await _dbContext.JobConfigurations
            .Where(j => j.IsActive)
            .ToListAsync();

        var serviceProvider = (IServiceProvider)context.MergedJobDataMap["ServiceProvider"];
        var scheduler = context.Scheduler;

        foreach (var jobConfig in jobsToSchedule)
        {
          try
          {
            await DynamicJobScheduler.ScheduleJob(scheduler, serviceProvider, jobConfig);
          }
          catch (Exception e)
          {
            _logger.LogError($"Error scheduling job {jobConfig.JobName}: {e.Message}");
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error in Dynamic Job Scheduler: {e.Message}");
      }

      _logger.LogInformation("Completed Dynamic Job Scheduler");
    }

    private static void RegisterJob(IServiceCollection services, JobConfiguration jobConfig)
    {
      var jobType = GetJobType(jobConfig);
      services.AddQuartzJob(jobType, CreateJobKey(jobConfig), jobConfig.JobDescription);
    }

    private static async Task ScheduleJob(IScheduler scheduler, IApplicationBuilder app, JobConfiguration jobConfig)
    {
      await ScheduleJobInternal(scheduler, jobConfig, (jobType, triggerBuilder) =>
      {
        app.UseQuartzJob(jobType, triggerBuilder);
      });
    }

    public static void RegisterWithQuartz(IServiceCollection services, NpgSqlContext dbContext)
    {
      RegisterJob(services, _jobConfig);

      var jobs = dbContext.JobConfigurations
          .ToList();

      foreach (var jobConfig in jobs)
      {
        RegisterJob(services, jobConfig);
      }
    }

    private static async Task ScheduleJob(IScheduler scheduler, IServiceProvider serviceProvider, JobConfiguration jobConfig)
    {
      await ScheduleJobInternal(scheduler, jobConfig, (jobType, triggerBuilder) =>
      {
        var scheduleJobs = serviceProvider.GetService<IEnumerable<IScheduleJob>>();
        IJobRegistratorExtensions.UseQuartzJob(scheduleJobs, jobType, triggerBuilder);
      });
    }

    private static async Task ScheduleJobInternal(IScheduler scheduler, JobConfiguration jobConfig, Action<Type, TriggerBuilder> scheduleAction)
    {
      JobKey jobKey = CreateJobKey(jobConfig);
      TriggerKey triggerKey = CreateTriggerKey(jobConfig);
      TriggerBuilder triggerBuilder = CreateTriggerBuilder(jobConfig);

      if (await scheduler.CheckExists(jobKey))
      {
        ITrigger trigger = await scheduler.GetTrigger(triggerKey);
        if (
          trigger is ICronTrigger cronTrigger &&
          cronTrigger.CronExpressionString != jobConfig.CronExpression
        )
        {
          await scheduler.RescheduleJob(triggerKey, triggerBuilder.Build());
          Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{jobConfig.CronExpression}]");
        }
      }
      else
      {
        var jobType = GetJobType(jobConfig);
        scheduleAction(jobType, triggerBuilder);
        Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{jobConfig.CronExpression}]");
      }
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      await ScheduleJob(scheduler, app, _jobConfig);
    }
  }
}

