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
  public class ApportionedEmissionsBulkData : IJob
  {
    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Apportioned Emissions Bulk Data";
      public static readonly string JobDescription = "Determine which emissions need to be regenerated and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run nightly and check which emissions files need to be regenerated";
      public static readonly string TriggerDescription = "Runs nightly to determine if files need to be regenerated based on query results or end of reporting quarter";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<ApportionedEmissionsBulkData>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        if(Utils.Configuration["EASEY_QUARTZ_SCHEDULER_APPORTIONED_EMISSIONS_SCHEDULE"] != null){
          app.UseQuartzJob<ApportionedEmissionsBulkData>(WithCronSchedule(Utils.Configuration["EASEY_QUARTZ_SCHEDULER_APPORTIONED_EMISSIONS_SCHEDULE"]));
        }
        else
          app.UseQuartzJob<ApportionedEmissionsBulkData>(WithCronSchedule("0 0/10 4-8 ? * * *"));
      }
    }

    public ApportionedEmissionsBulkData(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      
      

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Apportioned Emissions' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        return; // Job already exists , do not run again
      }

      if(Utils.Configuration["EASEY_EMISSIONS_NIGHTLY_BYPASS"] != "true"){
        // Does emissions nightly exists for current date and has it completed
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Emissions Nightly' AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          return;
        }
      }

      

      LogHelper.info("Executing ApportionedEmissionsBulkDataFiles job");

      JobLog jl = new JobLog(); 

      try
      {
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Apportioned Emissions";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();

        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerState = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_annual_emissions_bulk_files_per_state_to_generate", 2);
        List<List<Object>> rowsPerQuarter = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_annual_emissions_bulk_files_per_quarter_to_generate", 4);
        
        for(int row = 0; row < rowsPerState.Count; row++){
          decimal year = (decimal) rowsPerState[row][0];
          DateTime currentDate = DateTime.Now.ToUniversalTime();

          if( Math.Ceiling(currentDate.Year - year) > 1 || ( Math.Ceiling(currentDate.Year - year) == 1 && currentDate.Month > 1)){ // Past year, or last year after january            
            string stateCd = (string) rowsPerState[row][1];
            string urlParams = "beginDate=" + year + "-01-01&endDate=" + year + "-12-31&stateCode=" + stateCd;

            await _dbContext.CreateBulkFileJob(year, null, stateCd, "Emissions", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/hourly?" + urlParams, "emissions/hourly/state/emissions-hourly-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
            await _dbContext.CreateBulkFileJob(year, null, stateCd, "Emissions", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] +  "/emissions/apportioned/daily?" + urlParams, "emissions/daily/state/emissions-daily-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
            if(year >= 2015){ // MATS data started in 2015
              await _dbContext.CreateBulkFileJob(year, null, stateCd, "Mercury and Air Toxics Emissions (MATS)", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/state/mats-hourly-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
            }
          }
        }
        
        for(int row = 0; row < rowsPerQuarter.Count; row++){
          decimal year = (decimal) rowsPerQuarter[row][0];
          decimal quarter = (decimal) rowsPerQuarter[row][1];
          NpgsqlTypes.NpgsqlDate startDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][2];
          NpgsqlTypes.NpgsqlDate endDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][3];
          string urlParams = "beginDate=" + startDate.ToString() + "&endDate=" + endDate.ToString();

          await _dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/hourly?" + urlParams, "emissions/hourly/quarter/emissions-hourly-" + year + "-q" + quarter + ".csv", job_id, null);
          await _dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/daily?" + urlParams, "emissions/daily/quarter/emissions-daily-" + year + "-q" + quarter + ".csv", job_id, null);
          if(year >= 2015){ // MATS data started in 2015
            await _dbContext.CreateBulkFileJob(year, quarter, null, "Mercury and Air Toxics Emissions (MATS)", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/quarter/mats-hourly-" + year + "-q" + quarter + ".csv", job_id, null);
          }
        }

        
        _dbContext.ExecuteSql("CALL camdaux.procedure_set_dm_emissions_user();");

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();

        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executed ApportionedEmissionsBulkDataFiles job successfully");
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
      return JobBuilder.Create<ApportionedEmissionsBulkData>()
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