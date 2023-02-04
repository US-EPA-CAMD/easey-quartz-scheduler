using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkFileJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Bulk File Job Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if files in queue can be triggered.";
      public static readonly string TriggerName = "Check file queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in queue which can be triggered";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkFileJobQueue>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {

      if(await scheduler.CheckExists(WithJobKey())){
        await scheduler.DeleteJob(WithJobKey());
      }

      app.UseQuartzJob<BulkFileJobQueue>(WithCronSchedule("0 0/1 * 1/1 * ? *"));
      
    }

    public BulkFileJobQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");
        List<List<Object>>  bulkFileQueued = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log where job_class = 'Bulk Data File' AND status_cd = 'QUEUED'", 9);
        List<List<Object>>  bulkFileWorkinProgress = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log where job_class = 'Bulk Data File' AND status_cd = 'WIP'", 9);

        if(bulkFileWorkinProgress.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"])){
          if(bulkFileQueued.Count > 0){
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"]) - bulkFileWorkinProgress.Count;
            Console.WriteLine("Scheduling Jobs: " + jobs_to_schedule);
            int index = 0;
            for(int i = 0; i < jobs_to_schedule; i++){
              if(index < bulkFileQueued.Count){
                Guid idToSchedule = (Guid) bulkFileQueued[index][0];
                await context.Scheduler.TriggerJob(new JobKey(idToSchedule.ToString()));
                Thread.Sleep(5000);
                index++;
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
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}