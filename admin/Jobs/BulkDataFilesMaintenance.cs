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

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFileMaintenance : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public IConfiguration Configuration { get; }

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
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<BulkDataFileMaintenance>(WithCronSchedule("0 0 * * * ?"));
      }
    }

    public BulkDataFileMaintenance(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
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
        jl.AddDate = DateTime.Now;
        jl.StartDate = DateTime.Now;
        jl.EndDate = null;
        jl.StatusCd = "WIP";
        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();

        _dbContext.ExecuteSql("DELETE FROM camdaux.job_log WHERE job_id IN (SELECT job_id FROM camdaux.vw_bulk_file_jobs_to_delete);");

        List<List<Object>> rows = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_bulk_file_jobs_to_process", 16);

        for(int row = 0; row < rows.Count; row++){
          JobKey lookupKey = new JobKey((string) rows[row][0], Constants.QuartzGroups.BULK_DATA);

          IJobDetail jobToProcess = await context.Scheduler.GetJobDetail(lookupKey);
          if(jobToProcess != null){
            await context.Scheduler.DeleteJob(lookupKey);
            await context.Scheduler.ScheduleJob(jobToProcess, TriggerBuilder.Create().StartNow().Build());

            Console.Write("Reprocessed Job");
          }
        }

        jl.StatusCd = "COMPLETE";
        jl.EndDate = DateTime.Now;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();

      }catch(Exception e){
        jl.StatusCd = "ERROR";
        jl.EndDate = DateTime.Now;
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
          .WithCronSchedule(cronExpression);
    }
  }
}