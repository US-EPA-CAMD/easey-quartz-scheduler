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
using System.Linq;
using Microsoft.EntityFrameworkCore;

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
      try {
        JobKey jobKey = WithJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_BULK_FILE_QUEUE_SCHEDULE"] ?? "0 0/1 * 1/1 * ? *";

        if(await scheduler.CheckExists(jobKey)){
          Console.WriteLine($"Deleting {jobKey.Name} Job");
          await scheduler.DeleteJob(jobKey);
        }

        Console.WriteLine($"Attempting to schedule {jobKey.Name} job");
        app.UseQuartzJob<BulkFileJobQueue>(WithCronSchedule(cronExpression));
        Console.WriteLine($"Scheduled {jobKey.Name} Job");
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
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

        List<BulkFileQueue> inQueue = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd = 'QUEUED'"
          ).ToList();

        List<BulkFileQueue> inWIP = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd = 'WIP'"
          ).ToList();


        if(inWIP.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"])){
          if(inQueue.Count > 0){
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"]) - inWIP.Count;
            Console.WriteLine("Scheduling Jobs: " + jobs_to_schedule);
            int index = 0;
            for(int i = 0; i < jobs_to_schedule; i++){
              if(index < inQueue.Count){
                await BulkDataFile.CreateAndScheduleJobDetail(inQueue[i]);
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