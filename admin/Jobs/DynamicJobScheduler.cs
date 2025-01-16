/// <summary>
/// Provides the implementation for the dynamic job scheduler, which operates at regular intervals to determine if any new jobs need to be executed, scheduled, or rescheduled.
/// </summary>
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
  /// <summary>
  /// Represents a job scheduler that dynamically schedules or reschedules jobs based on database configurations.
  /// </summary>
  public class DynamicJobScheduler : IJob
  {
    private readonly NpgSqlContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DynamicJobScheduler> _logger;

    /// <summary>
    /// Static job configuration for the dynamic job scheduler.
    /// </summary>
    private static readonly JobConfiguration s_jobConfig = new JobConfiguration
    {
      JobName = "Dynamic Job Scheduler",
      JobDescription = "Operates on an interval to determine if any new jobs need to be executed, scheduled, or rescheduled",
      JobGroup = Constants.QuartzGroups.QUARTZ,
      JobType = "DynamicJobScheduler",
      TriggerName = "Check job queue every minute",
      TriggerDescription = "Operate every minute to determine if there are any new jobs to be scheduled or rescheduled",
      CronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_DYNAMIC_JOB_SCHEDULER_SCHEDULE"] ?? "0 0/1 * * * ?",
      IsActive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicJobScheduler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public DynamicJobScheduler(NpgSqlContext dbContext, IConfiguration configuration, ILogger<DynamicJobScheduler> logger)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _logger = logger;
    }

    /// <summary>
    /// Creates a new <see cref="JobKey"/> based on the provided job configuration.
    /// </summary>
    /// <param name="jobConfig">The job configuration.</param>
    /// <returns>A <see cref="JobKey"/> object.</returns>
    private static JobKey CreateJobKey(JobConfiguration jobConfig)
    {
      return new JobKey(jobConfig.JobName, jobConfig.JobGroup);
    }

    /// <summary>
    /// Creates a new <see cref="TriggerBuilder"/> based on the provided job configuration.
    /// </summary>
    /// <param name="jobConfig">The job configuration.</param>
    /// <returns>A <see cref="TriggerBuilder"/> object.</returns>
    /// <exception cref="FormatException">Thrown if the cron expression is invalid.</exception>
    private static TriggerBuilder CreateTriggerBuilder(JobConfiguration jobConfig)
    {
      if (!CronExpression.IsValidExpression(jobConfig.CronExpression))
      {
        throw new FormatException($"Invalid cron expression: {jobConfig.CronExpression}");
      }
      return TriggerBuilder.Create()
        .WithIdentity(CreateTriggerKey(jobConfig))
        .WithDescription(jobConfig.TriggerDescription)
        .WithSchedule(CronScheduleBuilder.CronSchedule(jobConfig.CronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }

    /// <summary>
    /// Creates a new <see cref="TriggerKey"/> based on the provided job configuration.
    /// </summary>
    /// <param name="jobConfig">The job configuration.</param>
    /// <returns>A <see cref="TriggerKey"/> object.</returns>
    private static TriggerKey CreateTriggerKey(JobConfiguration jobConfig)
    {
      return new TriggerKey(jobConfig.TriggerName ?? jobConfig.JobName, jobConfig.JobGroup);
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> of the job specified in the job configuration.
    /// </summary>
    /// <param name="jobConfig">The job configuration.</param>
    /// <returns>The <see cref="Type"/> of the job.</returns>
    /// <exception cref="Exception">Thrown if the job type cannot be found.</exception>
    private static Type GetJobType(JobConfiguration jobConfig)
    {
      var jobType = Type.GetType($"Epa.Camd.Quartz.Scheduler.Jobs.{jobConfig.JobType}");
      if (jobType == null)
      {
        throw new Exception($"Job type {jobConfig.JobType} not found");
      }
      return jobType;
    }

    /// <summary>
    /// Executes the dynamic job scheduler logic to schedule or reschedule jobs.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Starting Dynamic Job Scheduler");

      try
      {
        var jobsToSchedule = await _dbContext.JobConfigurations
            .Where(j => j.IsActive)
            .ToListAsync();
        _logger.LogInformation($"Found {jobsToSchedule.Count} jobs to schedule");

        var serviceProvider = (IServiceProvider)context.MergedJobDataMap["ServiceProvider"];
        var scheduler = context.Scheduler;

        foreach (var jobConfig in jobsToSchedule)
        {
          try
          {
            // Schedule the job according to its configured cron expression.
            await ScheduleJob(scheduler, serviceProvider, jobConfig);

            if (jobConfig.RunOnce == true)
            {
              var runAt = jobConfig.RunAt;

              // Toggle the `RunOnce` flag to prevent the job from running again.
              jobConfig.RunOnce = false;
              jobConfig.RunAt = null;
              _dbContext.JobConfigurations.Update(jobConfig);
              await _dbContext.SaveChangesAsync();

              // Schedule the job to run once at a specified time.
              await ScheduleJobOnce(scheduler, CreateJobKey(jobConfig), CreateTriggerKey(jobConfig), runAt);
            }
          }
          catch (Exception e)
          {
            _logger.LogError($"Error scheduling job {jobConfig.JobName}: {e.Message}");
          }
        }
      }
      catch (Exception e)
      {
        _logger.LogError($"Error in Dynamic Job Scheduler: {e.Message}");
      }

      _logger.LogInformation("Completed Dynamic Job Scheduler");
    }

    /// <summary>
    /// Registers a job with the specified services based on the job configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="jobConfig">The job configuration.</param>
    private static void RegisterJob(IServiceCollection services, JobConfiguration jobConfig)
    {
      var jobType = GetJobType(jobConfig);
      services.AddQuartzJob(jobType, CreateJobKey(jobConfig), jobConfig.JobDescription);
    }

    /// <summary>
    /// Registers all jobs with Quartz based on database configurations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbContext">The database context.</param>
    public static void RegisterWithQuartz(IServiceCollection services, NpgSqlContext dbContext)
    {
      RegisterJob(services, s_jobConfig);

      var jobs = dbContext.JobConfigurations.ToList();

      foreach (var jobConfig in jobs)
      {
        RegisterJob(services, jobConfig);
      }
    }

    /// <summary>
    /// Schedules a job using the provided scheduler, application builder, and logger.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="app">The application builder.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobConfig">The job configuration.</param>
    private static async Task ScheduleJob(IScheduler scheduler, IApplicationBuilder app, ILogger logger, JobConfiguration jobConfig)
    {
      await ScheduleJobInternal(scheduler, jobConfig, logger, (jobType, triggerBuilder) =>
      {
        app.UseQuartzJob(jobType, triggerBuilder);
      });
    }

    /// <summary>
    /// Schedules a job using the provided scheduler, service provider, and job configuration.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="jobConfig">The job configuration.</param>
    private async Task ScheduleJob(IScheduler scheduler, IServiceProvider serviceProvider, JobConfiguration jobConfig)
    {
      await ScheduleJobInternal(scheduler, jobConfig, _logger, (jobType, triggerBuilder) =>
      {
        var scheduleJobs = serviceProvider.GetService<IEnumerable<IScheduleJob>>();
        IJobRegistratorExtensions.UseQuartzJob(scheduleJobs, jobType, triggerBuilder);
      });
    }

    /// <summary>
    /// Internal method to schedule a job using the specified scheduler, job configuration, logger, and schedule action.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="jobConfig">The job configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="scheduleAction">The action to schedule the job.</param>
    private static async Task ScheduleJobInternal(IScheduler scheduler, JobConfiguration jobConfig, ILogger logger, Action<Type, TriggerBuilder> scheduleAction)
    {
      if (string.IsNullOrEmpty(jobConfig.CronExpression))
      {
        logger.LogWarning($"Job {jobConfig.JobName} has no cron expression and will not be scheduled");
        return;
      }

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
          logger.LogInformation($"Rescheduled {jobKey.Name} with cron expression [{jobConfig.CronExpression}]");
        }
      }
      else
      {
        var jobType = GetJobType(jobConfig);
        scheduleAction(jobType, triggerBuilder);
        logger.LogInformation($"Scheduled {jobKey.Name} with cron expression [{jobConfig.CronExpression}]");
      }
    }

    /// <summary>
    /// Schedules a job to run once, immediately or at a specified time.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="jobKey">The job key.</param>
    /// <param name="triggerKey">The trigger key.</param>
    /// <param name="runAt">The time to run the job, or null to run immediately.</param>
    private async Task ScheduleJobOnce(IScheduler scheduler, JobKey jobKey, TriggerKey triggerKey, DateTime? runAt = null)
    {
      // Retrieve the job detail using the JobKey.
      IJobDetail jobDetail = await scheduler.GetJobDetail(jobKey);
      if (jobDetail == null)
      {
        throw new Exception($"Job with key {jobKey.Name} not found.");
      }

      // Create the trigger to run once, immediately or at a specified time.
      TriggerBuilder triggerBuilder = TriggerBuilder.Create()
          .WithIdentity(triggerKey)
          .WithDescription($"Run once trigger for job {jobKey.Name}");

      if (runAt.HasValue)
      {
        // Ensure `runAt` is in Eastern Time.
        TimeZoneInfo easternZone = Utils.getCurrentEasternZone();
        DateTimeOffset easternTime = new DateTimeOffset(runAt.Value, easternZone.GetUtcOffset(runAt.Value));
        triggerBuilder.StartAt(easternTime);
        _logger.LogInformation($"Scheduling job {jobKey.Name} to run once at {easternTime}");
      }
      else
      {
        triggerBuilder.StartNow(); // Schedule the job to run immediately
        _logger.LogInformation($"Scheduling job {jobKey.Name} to run immediately");
      }

      // Schedule the job with the one-time trigger
      await scheduler.ScheduleJob(jobDetail, triggerBuilder.Build());
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app, ILogger logger)
    {
      await ScheduleJob(scheduler, app, logger, s_jobConfig);
    }
  }
}

