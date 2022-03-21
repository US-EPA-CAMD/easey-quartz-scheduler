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
      public static readonly string TriggerName = "Run nightly and check which files need to be regenerated";
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
        app.UseQuartzJob<ApportionedEmissionsBulkData>(WithCronSchedule("0 0 * * * ?"));
      }
    }

    public ApportionedEmissionsBulkData(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    private async Task<IJobDetail> CreateBulkFileJob(decimal year, decimal? quarter, string stateCd, string dataType, string subType, string url, string fileName){
      Guid child_job_id = Guid.NewGuid();

      JobLog jl = new JobLog();
      jl.JobId = child_job_id;
      jl.JobSystem = "Quartz";
      jl.JobClass = "Bulk Data File";
      jl.AddDate = DateTime.Now;
      jl.StartDate = null;
      jl.EndDate = null;
      jl.StatusCd = "QUEUED";

      BulkFileLog bfl = new BulkFileLog();
      bfl.JobId = child_job_id;
      bfl.ParentJobId = job_id;
      bfl.DataType = dataType;
      bfl.DataSubType = subType;
      bfl.Year = year;
      bfl.PrgCd = null;

      if(quarter != null){
        bfl.Quarter = quarter;
        bfl.StateCd = null;

        jl.JobName = dataType + "-" + subType + "-" + year + "-Q" + quarter;
      }
      else{
        bfl.Quarter = null;
        bfl.StateCd = stateCd;

        jl.JobName = dataType + "-" + subType + "-" + year + "-" + stateCd;
      }

      await _dbContext.JobLogs.AddAsync(jl);
      await _dbContext.SaveChangesAsync();

      await _dbContext.BulkFileLogs.AddAsync(bfl);
      await _dbContext.SaveChangesAsync();

      IJobDetail newJob = BulkDataFile.CreateJobDetail(child_job_id.ToString());
      newJob.JobDataMap.Add("job_id", child_job_id);
      newJob.JobDataMap.Add("parent_job_id", job_id);
      newJob.JobDataMap.Add("format", "text/csv");
      newJob.JobDataMap.Add("url", url);
      newJob.JobDataMap.Add("fileName", fileName);

      return newJob;
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
        jl.AddDate = DateTime.Now;
        jl.StartDate = DateTime.Now;
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

            await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, null, stateCd, "Emissions", "Hourly", "/apportioned/hourly/stream?" + urlParams, "emissions/hourly/state/Emissions-Hourly-" + year + "-" + stateCd + ".csv"), TriggerBuilder.Create().StartNow().Build());
            await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, null, stateCd, "Emissions", "Daily", "/apportioned/daily/stream?" + urlParams, "emissions/daily/state/Emissions-Daily-" + year + "-" + stateCd + ".csv"), TriggerBuilder.Create().StartNow().Build());
            await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, null, stateCd, "MATS", "Daily", "/apportioned/mats/hourly/stream?" + urlParams, "emissions/hourly/state/MATS-Hourly-" + year + "-" + stateCd + ".csv"), TriggerBuilder.Create().StartNow().Build());
          }
        }
        
        for(int row = 0; row < rowsPerQuarter.Count; row++){
          decimal year = (decimal) rowsPerQuarter[row][0];
          decimal quarter = (decimal) rowsPerQuarter[row][1];
          NpgsqlTypes.NpgsqlDate startDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][2];
          NpgsqlTypes.NpgsqlDate endDate = (NpgsqlTypes.NpgsqlDate) rowsPerQuarter[row][3];
          string urlParams = "beginDate=" + startDate.ToString() + "&endDate=" + endDate.ToString();

          await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, quarter, null, "Emissions", "Hourly", "/apportioned/hourly/stream?" + urlParams, "emissions/hourly/quarter/Emissions-Hourly-" + year + "-Q" + quarter + ".csv"), TriggerBuilder.Create().StartNow().Build());
          await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, quarter, null, "Emissions", "Daily", "/apportioned/daily/stream?" + urlParams, "emissions/daily/quarter/Emissions-Daily-" + year + "-Q" + quarter + ".csv"), TriggerBuilder.Create().StartNow().Build());
          await context.Scheduler.ScheduleJob(await CreateBulkFileJob(year, quarter, null, "MATS", "Hourly", "/apportioned/mats/hourly/stream?" + urlParams, "emissions/hourly/quarter/MATS-Hourly-" + year + "-Q" + quarter + ".csv"), TriggerBuilder.Create().StartNow().Build());
        }


        jl.StatusCd = "COMPLETE";
        jl.EndDate = DateTime.Now;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executed ApportionedEmissionsBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = DateTime.Now;
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