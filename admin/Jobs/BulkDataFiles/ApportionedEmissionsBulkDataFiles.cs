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

    public IConfiguration Configuration { get; }

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
        app.UseQuartzJob<ApportionedEmissionsBulkData>(WithCronSchedule("0 0 0 1/1 * ? *"));
      }
    }

    public ApportionedEmissionsBulkData(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing ApportionedEmissionsBulkDataFiles job");

      JobLog jl = new JobLog(); 

      try
      {
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Apportioned Emissions";
        jl.AddDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        jl.StartDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerState = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_annual_emissions_bulk_files_per_state_to_generate", 2);
        List<List<Object>> rowsPerQuarter = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_annual_emissions_bulk_files_per_quarter_to_generate", 4);
        
        /*
          decimal year = 2020;
          decimal quarter = 4;
          string startDate = "2020-10-01";
          string endDate = "2020-12-31";
          string urlParams = "beginDate=" + startDate.ToString() + "&endDate=" + endDate.ToString();        
          await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", "Hourly", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/hourly/stream?" + urlParams, "emissions/hourly/quarter/emissions-hourly-" + year + "-q" + quarter + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
        */
      
        
        for(int row = 0; row < rowsPerState.Count; row++){
          decimal year = (decimal) rowsPerState[row][0];
          DateTime currentDate = DateTime.Now.ToUniversalTime();

          if( Math.Ceiling(currentDate.Year - year) > 1 || ( Math.Ceiling(currentDate.Year - year) == 1 && currentDate.Month > 1)){ // Past year, or last year after january            
            string stateCd = (string) rowsPerState[row][1];
            string urlParams = "beginDate=" + year + "-01-01&endDate=" + year + "-12-31&stateCode=" + stateCd;

            await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, null, stateCd, "Emissions", "Hourly", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/hourly/stream?" + urlParams, "emissions/hourly/state/emissions-hourly-" + year + "-" + stateCd + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
            await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, null, stateCd, "Emissions", "Daily",Configuration["EASEY_EMISSIONS_API"] +  "/apportioned/daily/stream?" + urlParams, "emissions/daily/state/emissions-daily-" + year + "-" + stateCd + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
            await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, null, stateCd, "MATS", "Daily", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/mats/hourly/stream?" + urlParams, "mats/hourly/state/mats-hourly-" + year + "-" + stateCd + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
          }
        }
        
        for(int row = 0; row < rowsPerQuarter.Count; row++){
          decimal year = (decimal) rowsPerQuarter[row][0];
          decimal quarter = (decimal) rowsPerQuarter[row][1];
          NpgsqlTypes.NpgsqlDate startDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][2];
          NpgsqlTypes.NpgsqlDate endDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][3];
          string urlParams = "beginDate=" + startDate.ToString() + "&endDate=" + endDate.ToString();

          await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", "Hourly", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/hourly/stream?" + urlParams, "emissions/hourly/quarter/emissions-hourly-" + year + "-q" + quarter + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
          await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", "Daily", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/daily/stream?" + urlParams, "emissions/daily/quarter/emissions-daily-" + year + "-q" + quarter + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
          await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(year, quarter, null, "MATS", "Hourly", Configuration["EASEY_EMISSIONS_API"] + "/apportioned/mats/hourly/stream?" + urlParams, "mats/hourly/quarter/mats-hourly-" + year + "-q" + quarter + ".csv", job_id, null), TriggerBuilder.Create().StartNow().Build());
        }
        
        
        _dbContext.ExecuteSql("CALL camdaux.procedure_set_dm_emissions_user();");

        jl.StatusCd = "COMPLETE";
        jl.EndDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executed ApportionedEmissionsBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
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