using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using Epa.Camd.Logger;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class EvaluationJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;
    private readonly ILogger<EvaluationJobQueue> _logger;
    private IConfiguration Configuration { get; }

    public static class EvaluationJobQueueIdentity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Evaluation Job Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if files in evaluation queue can be triggered.";
      public static readonly string TriggerName = "Check evaluation queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in evaluation queue which can be triggered";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<EvaluationJobQueue>(WithEvaluationJobQueueJobKey(), EvaluationJobQueueIdentity.JobDescription);
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithEvaluationJobQueueJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_EVALUATION_QUEUE_SCHEDULE"] ?? "0 0/1 * 1/1 * ? *";
        TriggerBuilder triggerBuilder = WithEvaluationJobQueueCronSchedule(cronExpression);

        if (await scheduler.CheckExists(jobKey)) {
          ITrigger trigger = await scheduler.GetTrigger(WithEvaluationJobQueueTriggerKey());

          if (
            trigger is ICronTrigger cronTrigger &&
            cronTrigger.CronExpressionString != cronExpression
          ) {
            await scheduler.RescheduleJob(WithEvaluationJobQueueTriggerKey(), triggerBuilder.Build());
            Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
          }
        } else {
          app.UseQuartzJob<EvaluationJobQueue>(triggerBuilder);
          Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
        }
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public EvaluationJobQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<EvaluationJobQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        string instanceIndex = Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX") ?? "unknown";

        try
        {
            _logger.LogInformation("[Instance {InstanceIndex}] Starting evaluation queue check", instanceIndex);
            
            string[] types = new string[]{"MP", "QA", "EM"};

            foreach(string type in types){
                _logger.LogInformation("[Instance {InstanceIndex}] Checking {Type} evaluations", instanceIndex, type);
                
                List<Evaluation> inQueue = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'QUEUED'
                    ORDER BY queued_time", type
                ).ToList();

                _logger.LogInformation("[Instance {InstanceIndex}] Found {Count} {Type} evaluations in QUEUED status", 
                    instanceIndex, inQueue.Count, type);

                List<Evaluation> wip = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'WIP'
                    ORDER BY queued_time", type
                ).ToList();

                _logger.LogInformation("[Instance {InstanceIndex}] Found {Count} {Type} evaluations in WIP status", 
                    instanceIndex, wip.Count, type);

                int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + type +"_EVALUATIONS"]);
                _logger.LogInformation("[Instance {InstanceIndex}] Max allowed {Type} evaluations: {MaxAllowed}", 
                    instanceIndex, type, maxAllowed);

                if(wip.Count < maxAllowed){
                    if(inQueue.Count > 0){
                        int jobs_to_schedule = maxAllowed - wip.Count;
                        _logger.LogInformation("[Instance {InstanceIndex}] Attempting to schedule {JobCount} {Type} evaluations", 
                            instanceIndex, jobs_to_schedule, type);

                        for(int i = 0; i < jobs_to_schedule; i++){
                            if(i < inQueue.Count){
                                Evaluation toSchedule = inQueue[i];
                                _logger.LogInformation("[Instance {InstanceIndex}] Processing evaluation ID {EvalId}", 
                                    instanceIndex, toSchedule.EvaluationId);
                                
                                EvaluationSet es = _dbContext.EvaluationSet.Find(toSchedule.EvaluationSetId);
                                
                                _logger.LogInformation("[Instance {InstanceIndex}] Starting CheckEngineEvaluation for ID {EvalId}", 
                                    instanceIndex, toSchedule.EvaluationId);
                                await CheckEngineEvaluation.StartNow(
                                    context.Scheduler,
                                    toSchedule.EvaluationId,
                                    es.SetId,
                                    toSchedule.ProcessCode,
                                    es.FacId,
                                    es.FacName,
                                    es.MonPlanId,
                                    es.Config,
                                    es.UserId,
                                    es.UserEmail,
                                    toSchedule.QueuedTime,
                                    toSchedule.TestSumId,
                                    toSchedule.QaCertEventId,
                                    toSchedule.TeeId,
                                    toSchedule.RptPeriod
                                );

                                int delaySeconds = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_EVALUATION_JOB_QUEUE_DELAY"] ?? "1");
                                _logger.LogInformation("[Instance {InstanceIndex}] Waiting {Delay}s before next evaluation", 
                                    instanceIndex, delaySeconds);
                                Thread.Sleep(delaySeconds * 1000);
                            }
                        }
                    }
                } else {
                    _logger.LogInformation("[Instance {InstanceIndex}] Maximum number of {Type} evaluations ({MaxAllowed}) already in progress", 
                        instanceIndex, type, maxAllowed);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("[Instance {InstanceIndex}] Error in evaluation queue: {ErrorMessage}", 
                instanceIndex, e.Message);
            return;
        }
    }

    public static JobKey WithEvaluationJobQueueJobKey()
    {
      return new JobKey(EvaluationJobQueueIdentity.JobName, EvaluationJobQueueIdentity.Group);
    }

    public static TriggerKey WithEvaluationJobQueueTriggerKey()
    {
      return new TriggerKey(EvaluationJobQueueIdentity.TriggerName, EvaluationJobQueueIdentity.Group);
    }

    public static IJobDetail WithEvaluationJobQueueJobDetail()
    {
      return JobBuilder.Create<EvaluationJobQueue>()
          .WithIdentity(WithEvaluationJobQueueJobKey())
          .WithDescription(EvaluationJobQueueIdentity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithEvaluationJobQueueCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithEvaluationJobQueueTriggerKey())
          .WithDescription(EvaluationJobQueueIdentity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}
