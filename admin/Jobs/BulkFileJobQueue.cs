using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Epa.Camd.Logger;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkFileJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private static List<IJobDetail> bulk_data_queue = new List<IJobDetail>();

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Bulk File Job Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if files in queue can be triggered.";
      public static readonly string TriggerName = "Check file queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in queue which can be triggered";
    }

    public static void AddBulkDataJobToQueue(IJobDetail job){
      bulk_data_queue.Add(job);
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkFileJobQueue>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<BulkFileJobQueue>(WithCronSchedule("0 0/1 * 1/1 * ? *"));
      }
    }

    public BulkFileJobQueue(NpgSqlContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");
        List<List<Object>>  bulkFileWorkinProgress = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log where job_class = 'Bulk Data File' AND status_cd = 'WIP'", 9);
        Console.Write(bulkFileWorkinProgress.Count);
        if(bulkFileWorkinProgress.Count < 6){
          Console.Write(bulk_data_queue.Count);
          if(bulk_data_queue.Count > 0){
            int jobs_to_schedule = 6 - bulkFileWorkinProgress.Count;
            for(int i = 0; i < jobs_to_schedule; i++){
              if(bulk_data_queue.Count > 0){
                IJobDetail toSchedule = bulk_data_queue[0];
                await context.Scheduler.ScheduleJob(toSchedule, TriggerBuilder.Create().StartNow().Build());
                bulk_data_queue.Remove(toSchedule);
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
      return JobBuilder.Create<BulkFileJobQueue>()
          .WithIdentity(WithJobKey())
          .WithDescription(Identity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithTriggerKey())
          .WithDescription(Identity.TriggerDescription)
          .WithCronSchedule(cronExpression);
    }
  }
}