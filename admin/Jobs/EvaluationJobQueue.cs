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

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class EvaluationJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Evaluation Job Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if files in evaluation queue can be triggered.";
      public static readonly string TriggerName = "Check evaluation queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in queue which can be triggered";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<EvaluationJobQueue>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if(await scheduler.CheckExists(WithJobKey())){
        await scheduler.DeleteJob(WithJobKey());
      }
      
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<EvaluationJobQueue>(WithCronSchedule("0 0/1 * 1/1 * ? *"));
      }
    }

    public EvaluationJobQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");
        string[] types = new string[]{"MP", "QA", "EM"};

        foreach(string type in types){
          List<Evaluation> inQueue = _dbContext.Evaluations.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.evaluation_queue
            WHERE process_cd = {0} AND status_cd = 'QUEUED'
            ORDER BY submitted_on", type
          ).ToList();

          List<Evaluation> wip = _dbContext.Evaluations.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.evaluation_queue
            WHERE process_cd = {0} AND status_cd = 'WIP'
            ORDER BY submitted_on", type
          ).ToList();

          if(wip.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + type +"_EVALUATIONS"])){
            if(inQueue.Count > 0){
              int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + type +"_EVALUATIONS"]) - wip.Count;

              for(int i = 0; i < jobs_to_schedule; i++){
                if(i < inQueue.Count){
                  Evaluation toSchedule = inQueue[i];
                  EvaluationSet es = _dbContext.EvaluationSet.Find(toSchedule.EvaluationSetId);
                  
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
                    toSchedule.SubmittedOn,
                    toSchedule.TestSumId,
                    toSchedule.QaCertEventId,
                    toSchedule.TeeId,
                    toSchedule.RptPeriod
                  );
                }
              }
            }
          }

        }
        return;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return;
      }
    }

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

    public static TriggerKey WithTriggerKey()
    {
      return new TriggerKey(Identity.TriggerName, Identity.Group);
    }

    public static IJobDetail WithJobDetail()
    {
      return JobBuilder.Create<EvaluationJobQueue>()
          .WithIdentity(WithJobKey())
          .WithDescription(Identity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithTriggerKey())
          .WithDescription(Identity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}