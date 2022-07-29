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
  public class AllowanceHoldingsBulkDataFiles : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Allowance Holdings Bulk Data";
      public static readonly string JobDescription = "Generate Allowance Holdings and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run nightly and regen allowance holdings files";
      public static readonly string TriggerDescription = "Runs nightly to generate allowance holdings files";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<AllowanceHoldingsBulkDataFiles>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        if(Utils.Configuration["EASEY_QUARTZ_SCHEDULER_ALLOWANCE_HOLDINGS_SCHEDULE"] != null){
          app.UseQuartzJob<AllowanceHoldingsBulkDataFiles>(WithCronSchedule(Utils.Configuration["EASEY_QUARTZ_SCHEDULER_ALLOWANCE_HOLDINGS_SCHEDULE"]));
        }
        else
          app.UseQuartzJob<AllowanceHoldingsBulkDataFiles>(WithCronSchedule("0 0/10 1-5 ? * * *"));
      }
    }

    public AllowanceHoldingsBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing AllowanceHoldingsBulkDataFiles job");

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Holdings' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        return; // Job already exists , do not run again
      }

      
      // Does data mart nightly exists for current date and has it completed
      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          return;
        }
      }
      

      JobLog jl = new JobLog(); 

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Holdings";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> programCodeRows = await this._dbContext.ExecuteSqlQuery("SELECT prg_cd FROM camdmd.program_code pc WHERE pc.active = 1", 1);
        string[] programCodes = new string[programCodeRows.Count];

        for(int i = 0; i < programCodeRows.Count; i++){
          programCodes[i] = (string) programCodeRows[i][0];
        }
        
        for(int row = 0; row < programCodes.Length; row++){
          string code = programCodes[row];
          decimal year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "programCodeInfo=" + code;

          await _dbContext.CreateBulkFileJob(null, null, null, "Allowance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-holdings?" + urlParams, "allowance/holdings-" + code.ToLower() + ".csv", job_id, code);
        }
        
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executing AllowanceHoldingsBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.error(e.Message);
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
      return JobBuilder.Create<AllowanceHoldingsBulkDataFiles>()
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