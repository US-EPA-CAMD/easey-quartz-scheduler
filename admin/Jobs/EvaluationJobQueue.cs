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

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class EvaluationJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

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

    public EvaluationJobQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        string instanceIndex = Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX") ?? "unknown";

        try
        {
            LogHelper.info($"[Instance {instanceIndex}] Starting evaluation queue check");
            
            string[] types = new string[]{"MP", "QA", "EM"};

            foreach(string type in types){
                LogHelper.info($"[Instance {instanceIndex}] Checking {type} evaluations");
                
                List<Evaluation> inQueue = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'QUEUED'
                    ORDER BY queued_time", type
                ).ToList();

                LogHelper.info($"[Instance {instanceIndex}] Found {inQueue.Count} {type} evaluations in QUEUED status");

                List<Evaluation> wip = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'WIP'
                    ORDER BY queued_time", type
                ).ToList();

                LogHelper.info($"[Instance {instanceIndex}] Found {wip.Count} {type} evaluations in WIP status");

                int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + type +"_EVALUATIONS"]);
                LogHelper.info($"[Instance {instanceIndex}] Max allowed {type} evaluations: {maxAllowed}");

                if(wip.Count < maxAllowed){
                    if(inQueue.Count > 0){
                        int jobs_to_schedule = maxAllowed - wip.Count;
                        LogHelper.info($"[Instance {instanceIndex}] Attempting to schedule {jobs_to_schedule} {type} evaluations");

                        for(int i = 0; i < jobs_to_schedule; i++){
                            if(i < inQueue.Count){
                                Evaluation toSchedule = inQueue[i];
                                LogHelper.info($"[Instance {instanceIndex}] Processing evaluation ID {toSchedule.EvaluationId}");
                                
                                EvaluationSet es = _dbContext.EvaluationSet.Find(toSchedule.EvaluationSetId);
                                
                                LogHelper.info($"[Instance {instanceIndex}] Starting CheckEngineEvaluation for ID {toSchedule.EvaluationId}");
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
                                LogHelper.info($"[Instance {instanceIndex}] Waiting {delaySeconds}s before next evaluation");
                                Thread.Sleep(delaySeconds * 1000);
                            }
                        }
                    }
                } else {
                    LogHelper.info($"[Instance {instanceIndex}] Maximum number of {type} evaluations ({maxAllowed}) already in progress");
                }
            }
        }
        catch (Exception e)
        {
            LogHelper.error($"[Instance {instanceIndex}] Error in evaluation queue: {e.Message}");
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
