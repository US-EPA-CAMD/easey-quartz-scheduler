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
using Epa.Camd.Logger;
using System.Reflection;

using Quartz;
using SilkierQuartz;
using SilkierQuartz.HostedService;

using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public interface IJobMetadata<TJob> where TJob : IJob
  {
    static abstract string JobName { get; }
    static abstract string JobDescription { get; }
    static abstract string JobGroup { get; }
    static abstract string TriggerName { get; }
    static abstract string TriggerDescription { get; }
  }

  public class JobDescriptor
  {
    // The actual job type
    public Type JobType { get; init; } = default!;

    // From IJobMetadata<TJob>
    public string JobName { get; init; } = default!;
    public string JobDescription { get; init; } = default!;
    public string JobGroup { get; init; } = default!;
    public string TriggerName { get; init; } = default!;
    public string TriggerDescription { get; init; } = default!;

    // From JobConfiguration
    public string CronExpression { get; init; } = default!;
    public bool IsActive { get; init; }
    public bool RunOnce { get; init; }
    public DateTime? RunAt { get; init; }
  }

  public static class JobDescriptorFactory
  {
    public static JobDescriptor Create<TJob>(JobConfiguration jobConfig)
      where TJob : IJob, IJobMetadata<TJob>
    {
      return new JobDescriptor
      {
        JobType = typeof(TJob),
        JobName = TJob.JobName,
        JobDescription = TJob.JobDescription,
        JobGroup = TJob.JobGroup,
        TriggerName = TJob.TriggerName,
        TriggerDescription = TJob.TriggerDescription,
        CronExpression = jobConfig?.CronExpression ?? string.Empty,
        IsActive = jobConfig?.IsActive ?? false,
        RunOnce = jobConfig?.RunOnce ?? false,
        RunAt = jobConfig?.RunAt ?? null,
      };
    }

    public static JobDescriptor Create(Type jobType, JobConfiguration config)
    {
      // figure out the IJobMetadata<TJob> interface
      var metaInterface = jobType.GetInterfaces()
          .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJobMetadata<>));

      string jobName = (string)metaInterface.GetProperty("JobName")!.GetValue(null)!;
      string jobDescription = (string)metaInterface.GetProperty("JobDescription")!.GetValue(null)!;
      string jobGroup = (string)metaInterface.GetProperty("JobGroup")!.GetValue(null)!;
      string triggerName = (string)metaInterface.GetProperty("TriggerName")!.GetValue(null)!;
      string triggerDescription = (string)metaInterface.GetProperty("TriggerDescription")!.GetValue(null)!;

      return new JobDescriptor
      {
        JobType = jobType,
        JobName = jobName,
        JobDescription = jobDescription,
        JobGroup = jobGroup,
        TriggerName = triggerName,
        TriggerDescription = triggerDescription,
        CronExpression = config?.CronExpression ?? string.Empty,
        IsActive = config?.IsActive ?? false,
        RunOnce = config?.RunOnce ?? false,
        RunAt = config?.RunAt ?? null,
      };
    }

    public static class JobDiscovery
    {
      public static IEnumerable<Type> DiscoverJobs(Assembly assembly)
      {
        return assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IJobMetadata<>)
                )
            );
      }
    }


    public static class JobRegistry
    {
      public static List<JobDescriptor> BuildRegistry(
          Assembly assembly,
          IEnumerable<JobConfiguration> configs)
      {
        var jobs = JobDiscovery.DiscoverJobs(assembly);

        var factoryMethod = typeof(JobDescriptorFactory)
            .GetMethod(nameof(JobDescriptorFactory.Create))!;

        return jobs
          .Select(jobType =>
          {
            string className = jobType.Name;
            var config = configs.FirstOrDefault(c => c.JobClass == className);

            // Make Create<jobType>() dynamically
            var generic = factoryMethod.MakeGenericMethod(jobType);
            return (JobDescriptor)generic.Invoke(null, new object[] { config })!;
          })
          .ToList();
      }
    }



    /// <summary>
    /// Represents a job scheduler that dynamically schedules or reschedules jobs based on database configurations.
    /// </summary>
    [DisallowConcurrentExecution]
    public class DynamicJobScheduler : IJob
    {
      private readonly NpgSqlContext _dbContext;
      private readonly IConfiguration _configuration;
      private readonly ILogger<DynamicJobScheduler> _logger;
      private readonly IServiceProvider _serviceProvider;

      /// <summary>
      /// Static job descriptor for the dynamic job scheduler.
      /// </summary>
      private static readonly JobDescriptor s_jobDescriptor = new JobDescriptor
      {
        JobName = "Dynamic Job Scheduler",
        JobDescription = "Operates on an interval to determine if any new jobs need to be executed, scheduled, or rescheduled",
        JobGroup = Constants.QuartzGroups.QUARTZ,
        JobType = typeof(DynamicJobScheduler),
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
      /// <param name="jobDescriptor">The job metadata and configuration.</param>
      /// <returns>A <see cref="JobKey"/> object.</returns>
      private static JobKey CreateJobKey(JobDescriptor jobDescriptor)
      {
        return new JobKey(jobDescriptor.JobName, jobDescriptor.JobGroup);
      }

      /// <summary>
      /// Creates a new <see cref="TriggerBuilder"/> based on the provided job configuration.
      /// </summary>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      /// <returns>A <see cref="TriggerBuilder"/> object.</returns>
      /// <exception cref="FormatException">Thrown if the cron expression is invalid.</exception>
      private static TriggerBuilder CreateTriggerBuilder(JobDescriptor jobDescriptor)
      {
        if (!CronExpression.IsValidExpression(jobDescriptor.CronExpression))
        {
          throw new FormatException($"Invalid cron expression: {jobDescriptor.CronExpression}");
        }
        return TriggerBuilder.Create()
          .WithIdentity(CreateTriggerKey(jobDescriptor))
          .WithDescription(jobDescriptor.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(jobDescriptor.CronExpression).InTimeZone(Utils.getCurrentEasternZone()));
      }

      /// <summary>
      /// Creates a new <see cref="TriggerKey"/> based on the provided job configuration.
      /// </summary>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      /// <returns>A <see cref="TriggerKey"/> object.</returns>
      private static TriggerKey CreateTriggerKey(JobDescriptor jobDescriptor, bool once = false)
      {
        return new TriggerKey($"{jobDescriptor.TriggerName ?? jobDescriptor.JobName} ({(once ? "once" : "recurring")})", jobDescriptor.JobGroup);
      }

      /// <summary>
      /// Retrieves the <see cref="Type"/> of the job specified in the job configuration.
      /// </summary>
      /// <param name="jobConfig">The job configuration.</param>
      /// <returns>The <see cref="Type"/> of the job.</returns>
      private static Type GetJobType(JobConfiguration jobConfig)
      {
        return Type.GetType($"Epa.Camd.Quartz.Scheduler.Jobs.{jobConfig.JobClass}");
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
            JobDescriptor jobDescriptor = JobDescriptorFactory.Create(GetJobType(jobConfig), jobConfig);
            if (jobDescriptor.JobType == null)
            {
              _logger.LogError($"Job type for class {jobConfig.JobClass} not found.");
              continue;
            }

            if (jobConfig.IsActive)
            {
              try
              {
                // Schedule the job according to its configured cron expression.
                await ScheduleJob(scheduler, jobDescriptor);

                if (jobConfig.RunOnce == true)
                {
                  var runAt = jobConfig.RunAt;

                  // Toggle the `RunOnce` flag to prevent the job from running again.
                  jobConfig.RunOnce = false;
                  jobConfig.RunAt = null;
                  _dbContext.JobConfigurations.Update(jobConfig);
                  _dbContext.SaveChanges();

                  // Schedule the job to run once at a specified time.
                  await ScheduleJobOnce(scheduler, jobDescriptor, runAt);
                }
              }
              catch (Exception e)
              {
                _logger.LogError($"Error scheduling job {jobDescriptor.JobName}: {e.Message}");
              }
            }
            else
            {
              try
              {
                await UnscheduleJob(scheduler, jobDescriptor, _logger);
              }
              catch (Exception e)
              {
                _logger.LogError($"Error unscheduling job {jobDescriptor.JobName}: {e.Message}");
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
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      private static void RegisterJob(IServiceCollection services, JobDescriptor jobDescriptor)
      {
        ILogger<DynamicJobScheduler> staticLogger = LoggerProvider.GetLogger<DynamicJobScheduler>();
        var jobType = jobDescriptor.JobType;
        if (jobType != null)
        {
          services.AddQuartzJob(jobType, CreateJobKey(jobDescriptor), jobDescriptor.JobDescription);
          staticLogger.LogInformation("Registered job: {JobType} with Quartz", jobDescriptor.JobType);
        }
      }

      /// <summary>
      /// Registers all jobs with Quartz based on database configurations.
      /// </summary>
      /// <param name="services">The service collection.</param>
      /// <param name="jobDescriptors">A list of job descriptors to register.</param>
      public static void RegisterWithQuartz(IServiceCollection services, List<JobDescriptor> jobDescriptors)
      {
        RegisterJob(services, s_jobDescriptor);

        foreach (var jobDescriptor in jobDescriptors)
        {
          RegisterJob(services, jobDescriptor);
        }
      }

      /// <summary>
      /// Schedules a job using the provided scheduler, application builder, and logger.
      /// </summary>
      /// <param name="scheduler">The scheduler instance.</param>
      /// <param name="app">The application builder.</param>
      /// <param name="logger">The logger instance.</param>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      private static async Task ScheduleJob(IScheduler scheduler, IApplicationBuilder app, JobDescriptor jobDescriptor, ILogger logger)
      {
        await ScheduleJobInternal(scheduler, jobDescriptor, logger, (triggerBuilder) =>
        {
          return Task.Run(() => app.UseQuartzJob(jobDescriptor.JobType, triggerBuilder));
        });
      }

      /// <summary>
      /// Schedules a job using the provided scheduler, service provider, and job configuration.
      /// </summary>
      /// <param name="scheduler">The scheduler instance.</param>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      private async Task ScheduleJob(IScheduler scheduler, JobDescriptor jobDescriptor)
      {
        await ScheduleJobInternal(scheduler, jobDescriptor, _logger, async (triggerBuilder) =>
        {
          var scheduleJobs = _serviceProvider.GetService<IEnumerable<IScheduleJob>>();
          await IJobRegistratorExtensions.UseQuartzJob(scheduleJobs, scheduler, jobDescriptor.JobType, triggerBuilder);
        });
      }

      /// <summary>
      /// Internal method to schedule a job using the specified scheduler, job configuration, logger, and schedule action.
      /// </summary>
      /// <param name="scheduler">The scheduler instance.</param>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      /// <param name="logger">The logger instance.</param>
      /// <param name="scheduleAction">The action to schedule the job.</param>
      private static async Task ScheduleJobInternal(IScheduler scheduler, JobDescriptor jobDescriptor, ILogger logger, Func<TriggerBuilder, Task> scheduleAction)
      {
        if (string.IsNullOrEmpty(jobDescriptor.CronExpression))
        {
          // Unschedule the job if the cron expression is empty.
          await UnscheduleJob(scheduler, jobDescriptor, logger);
          return;
        }

        JobKey jobKey = CreateJobKey(jobDescriptor);
        TriggerKey triggerKey = CreateTriggerKey(jobDescriptor);
        TriggerBuilder triggerBuilder = CreateTriggerBuilder(jobDescriptor);

        if (await scheduler.CheckExists(jobKey))
        {
          if (await scheduler.CheckExists(triggerKey))
          {
            // Reschedule if the job's cron expression has changed.
            ITrigger trigger = await scheduler.GetTrigger(triggerKey);
            if (
              trigger is ICronTrigger cronTrigger &&
              cronTrigger.CronExpressionString != jobDescriptor.CronExpression
            )
            {
              await scheduler.RescheduleJob(triggerKey, triggerBuilder.Build());
              logger.LogInformation($"Rescheduled {jobKey.Name} with cron expression [{jobDescriptor.CronExpression}]");
            }
          }
          else
          {
            // Schedule the job with the new trigger.
            await scheduler.ScheduleJob(triggerBuilder.ForJob(jobKey).Build());
            logger.LogInformation($"Scheduled {jobKey.Name} with cron expression [{jobDescriptor.CronExpression}]");
          }
        }
        else
        {
          // Create a new job detail and schedule the job.
          await scheduleAction(triggerBuilder);
          logger.LogInformation($"Scheduled {jobKey.Name} with cron expression [{jobDescriptor.CronExpression}]");
        }
      }

      /// <summary>
      /// Schedules a job to run once, immediately or at a specified time.
      /// </summary>
      /// <param name="scheduler">The scheduler instance.</param>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      /// <param name="runAt">The time to run the job, or null to run immediately.</param>
      private async Task ScheduleJobOnce(IScheduler scheduler, JobDescriptor jobDescriptor, DateTime? runAt = null)
      {
        JobKey jobKey = CreateJobKey(jobDescriptor);
        TriggerKey triggerKey = CreateTriggerKey(jobDescriptor, once: true);

        // Retrieve the job detail using the JobKey.
        IJobDetail jobDetail = await scheduler.GetJobDetail(jobKey);
        if (jobDetail == null)
        {
          _logger.LogInformation($"Job with key {jobKey.Name} not found. Adding job to the scheduler.");

          // Create a new job detail.
          var jobType = jobDescriptor.JobType;
          if (jobType == null)
          {
            _logger.LogError($"Job type {jobDescriptor.JobType} not found.");
            return;
          }

          jobDetail = JobBuilder
            .Create(jobType)
            .WithIdentity(jobKey)
            .WithDescription(jobDescriptor.JobDescription)
            .StoreDurably(true) // Ensure job persists even without a trigger
            .Build();

          await scheduler.AddJob(jobDetail, replace: false); // Only add the job if it doesn't exist
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
          triggerBuilder.StartNow();
          _logger.LogInformation($"Scheduling job {jobKey.Name} to run immediately");
        }

        var newTrigger = triggerBuilder.ForJob(jobKey).Build();

        if (await scheduler.CheckExists(triggerKey))
        {
          await scheduler.RescheduleJob(triggerKey, newTrigger);
        }
        else
        {
          await scheduler.ScheduleJob(newTrigger);
        }
      }

      public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app, ILogger logger)
      {
        await ScheduleJob(scheduler, app, s_jobDescriptor, logger);
      }

      /// <summary>
      /// Unschedules a job from the scheduler based on the job configuration.
      /// </summary>
      /// <param name="scheduler">The scheduler instance.</param>
      /// <param name="jobDescriptor">The job configuration and metadata.</param>
      private static async Task UnscheduleJob(IScheduler scheduler, JobDescriptor jobDescriptor, ILogger logger)
      {
        TriggerKey triggerKey = CreateTriggerKey(jobDescriptor);

        if (await scheduler.CheckExists(triggerKey))
        {
          bool unscheduled = await scheduler.UnscheduleJob(triggerKey);
          if (unscheduled)
          {
            logger.LogInformation($"Successfully removed trigger for job: {jobDescriptor.JobName}");
          }
          else
          {
            logger.LogWarning($"Failed to remove trigger for job: {jobDescriptor.JobName}.");
          }
        }
      }
    }
  }
}

