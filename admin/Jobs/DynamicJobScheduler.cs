/// <summary>
/// Provides the implementation for the dynamic job scheduler, which operates at regular intervals to determine if any new jobs need to be executed, scheduled, or rescheduled.
/// </summary>
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
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
    private readonly IServiceProvider _serviceProvider;

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
    public DynamicJobScheduler(NpgSqlContext dbContext, IConfiguration configuration, ILogger<DynamicJobScheduler> logger, IServiceProvider serviceProvider)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _logger = logger;
      _serviceProvider = serviceProvider;
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
    private static TriggerKey CreateTriggerKey(JobConfiguration jobConfig, bool once = false)
    {
      return new TriggerKey($"{jobConfig.TriggerName ?? jobConfig.JobName} ({(once ? "once" : "recurring")})", jobConfig.JobGroup);
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> of the job specified in the job configuration.
    /// </summary>
    /// <param name="jobConfig">The job configuration.</param>
    /// <returns>The <see cref="Type"/> of the job.</returns>
    private static Type GetJobType(JobConfiguration jobConfig)
    {
      return Type.GetType($"Epa.Camd.Quartz.Scheduler.Jobs.{jobConfig.JobType}");
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
        var jobConfigs = _dbContext.JobConfigurations.ToList();
        _logger.LogInformation($"Found {jobConfigs.Count} job configurations");

        var scheduler = context.Scheduler;

        foreach (var jobConfig in jobConfigs)
        {
          if (jobConfig.IsActive)
          {
            try
            {
              // Schedule the job according to its configured cron expression.
              await ScheduleJob(scheduler, jobConfig);

              if (jobConfig.RunOnce == true)
              {
                var runAt = jobConfig.RunAt;

                // Toggle the `RunOnce` flag to prevent the job from running again.
                jobConfig.RunOnce = false;
                jobConfig.RunAt = null;
                _dbContext.JobConfigurations.Update(jobConfig);
                await _dbContext.SaveChangesAsync();

                // Schedule the job to run once at a specified time.
                await ScheduleJobOnce(scheduler, jobConfig, runAt);
              }
            }
            catch (Exception e)
            {
              _logger.LogError($"Error scheduling job {jobConfig.JobName}: {e.Message}");
            }
          }
          else
          {
            try
            {
              await UnscheduleJob(scheduler, jobConfig, _logger);
            }
            catch (Exception e)
            {
              _logger.LogError($"Error unscheduling job {jobConfig.JobName}: {e.Message}");
            }
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
      if (jobType != null)
      {
        services.AddQuartzJob(jobType, CreateJobKey(jobConfig), jobConfig.JobDescription);
        Console.WriteLine($"Registered job: {jobConfig.JobType} with Quartz"); // TODO: Replace with proper logging
      }
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
    private static async Task ScheduleJob(IScheduler scheduler, IApplicationBuilder app, JobConfiguration jobConfig, ILogger logger)
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
    /// <param name="jobConfig">The job configuration.</param>
    private async Task ScheduleJob(IScheduler scheduler, JobConfiguration jobConfig)
    {
      await ScheduleJobInternal(scheduler, jobConfig, _logger, (jobType, triggerBuilder) =>
      {
        var scheduleJobs = _serviceProvider.GetService<IEnumerable<IScheduleJob>>();
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
        await UnscheduleJob(scheduler, jobConfig, logger);
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
        if (jobType == null)
        {
          logger.LogError($"Job type {jobConfig.JobType} not found.");
          return;
        }
        scheduleAction(jobType, triggerBuilder);
        logger.LogInformation($"Scheduled {jobKey.Name} with cron expression [{jobConfig.CronExpression}]");
      }
    }

    /// <summary>
    /// Schedules a job to run once, immediately or at a specified time.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="jobConfig">The job configuration.</param>
    /// <param name="runAt">The time to run the job, or null to run immediately.</param>
    private async Task ScheduleJobOnce(IScheduler scheduler, JobConfiguration jobConfig, DateTime? runAt = null)
    {
      JobKey jobKey = CreateJobKey(jobConfig);
      TriggerKey triggerKey = CreateTriggerKey(jobConfig, once: true);

      // Retrieve the job detail using the JobKey.
      IJobDetail jobDetail = await scheduler.GetJobDetail(jobKey);
      if (jobDetail == null)
      {
        _logger.LogInformation($"Job with key {jobKey.Name} not found. Adding job to the scheduler.");

        // Create a new job detail.
        var jobType = GetJobType(jobConfig);
        if (jobType == null)
        {
          _logger.LogError($"Job type {jobConfig.JobType} not found.");
          return;
        }
        jobDetail = JobBuilder
          .Create(GetJobType(jobConfig))
          .WithIdentity(jobKey)
          .WithDescription(jobConfig.JobDescription)
          .Build();

        await scheduler.AddJob(jobDetail, replace: true); // Add the job to the scheduler
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

      if (await scheduler.CheckExists(triggerKey))
      {
        await scheduler.RescheduleJob(triggerKey, triggerBuilder.Build());
      }
      else
      {
        // Schedule the job with the one-time trigger
        await scheduler.ScheduleJob(jobDetail, triggerBuilder.Build());
      }
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app, ILogger logger)
    {
      await ScheduleJob(scheduler, app, s_jobConfig, logger);
    }

    /// <summary>
    /// Unschedules a job from the scheduler based on the job configuration.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="jobConfig">The job configuration.</param>
    private static async Task UnscheduleJob(IScheduler scheduler, JobConfiguration jobConfig, ILogger logger)
    {
      TriggerKey triggerKey = CreateTriggerKey(jobConfig);

      if (await scheduler.CheckExists(triggerKey))
      {
        bool unscheduled = await scheduler.UnscheduleJob(triggerKey);
        if (unscheduled)
        {
          logger.LogInformation($"Successfully removed trigger for job: {jobConfig.JobName}");
        }
        else
        {
          logger.LogWarning($"Failed to remove trigger for job: {jobConfig.JobName}.");
        }
      }
    }
  }
}

