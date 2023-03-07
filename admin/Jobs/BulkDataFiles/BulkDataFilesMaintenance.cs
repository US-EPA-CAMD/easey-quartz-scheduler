using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFileMaintenance : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Bulk Data File Maintenance";
      public static readonly string JobDescription = "Run a check on the bulk data file maintenance queue";
      public static readonly string TriggerName = "Run nightly and check which files need to have maintenance performed";
      public static readonly string TriggerDescription = "Run nightly and check which files need to get rerun or cleaned up";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkDataFileMaintenance>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_MAINTENANCE_SCHEDULE"] ?? "0 0 6 ? * * *";

        if(await scheduler.CheckExists(jobKey)){
          Console.WriteLine($"Deleting {jobKey.Name} Job");
          await scheduler.DeleteJob(jobKey);
        }

        Console.WriteLine($"Attempting to schedule {jobKey.Name} job");
        app.UseQuartzJob<BulkDataFileMaintenance>(WithCronSchedule(cronExpression));
        Console.WriteLine($"Scheduled {jobKey.Name} Job");
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public BulkDataFileMaintenance(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing BulkDataFileMaintenance job");

      JobLog jl = new JobLog();
      try{
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Bulk Data File Maintenance";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";
        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();

        List<BulkFileQueue> toDelete = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd IN('QUEUED', 'ERROR', 'WIP') AND add_date < now() - interval '30 days'"
          ).ToList();

        foreach (BulkFileQueue record in toDelete)
        {
            if(await context.Scheduler.CheckExists(new JobKey(record.JobId.ToString()))){
              await context.Scheduler.DeleteJob(new JobKey(record.JobId.ToString()));
            }
        }

        _dbContext.ExecuteSql("DELETE from camdaux.bulk_file_queue where add_date < now() - interval '30 days'");
        _dbContext.ExecuteSql("DELETE from camdaux.job_log where add_date < now() - interval '30 days'");

        _dbContext.ExecuteSql("CALL camdaux.procedure_bulk_file_requeue_check();");

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();

      }catch(Exception e){
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.error(e.Message);
      }

      LogHelper.info("Executed BulkDataFileMaintenance job");
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
      return JobBuilder.Create<BulkDataFileMaintenance>()
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