using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class ApportionedEmissionsBulkData : IJob
  {
    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<ApportionedEmissionsBulkData> _logger;

    public ApportionedEmissionsBulkData(NpgSqlContext dbContext, IConfiguration configuration, ILogger<ApportionedEmissionsBulkData> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing ApportionedEmissionsBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Apportioned Emissions' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
      _logger.LogWarning("Job already exists for today. Skipping execution. JobId: {JobId}", job_id);
        return; // Job already exists , do not run again
      }

      if(Utils.Configuration["EASEY_EMISSIONS_NIGHTLY_BYPASS"] != "true"){
        // Does emissions nightly exists for current date and has it completed
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Emission Nightly' AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          _logger.LogWarning("Emission Nightly has not completed. Skipping execution. JobId: {JobId}", job_id);
          return;
        }
      }

      _logger.LogInformation("Creating Apportioned Emissions JobLog. JobId: {JobId}", job_id);

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
          int year = Convert.ToInt32(rowsPerState[row][0]);
          DateTime currentDate = DateTime.Now.ToUniversalTime();

          string stateCd = (string)rowsPerState[row][1];
          string urlParams = "beginDate=" + year + "-01-01&endDate=" + year + "-12-31&stateCode=" + stateCd;

          _logger.LogInformation("Creating records for state-level emissions. Year: {Year}, State: {StateCd}", year, stateCd);

          await _dbContext.CreateBulkFileRecord("Hourly-Apportioned-Emissions-"+stateCd+"-"+year,  job_id,year, null, stateCd, "Emissions", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/hourly?" + urlParams, "emissions/hourly/state/emissions-hourly-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
          await _dbContext.CreateBulkFileRecord("Daily-Apportioned-Emissions-"+stateCd+"-"+year, job_id,year, null, stateCd, "Emissions", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] +  "/emissions/apportioned/daily?" + urlParams, "emissions/daily/state/emissions-daily-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
          if(year >= 2015){ // MATS data started in 2015
            _logger.LogInformation("Creating records for Hourly-MATS for year > 2015. Year: {Year}, State: {StateCd}", year, stateCd);
            await _dbContext.CreateBulkFileRecord("Hourly-MATS-"+stateCd+"-"+year, job_id,year, null, stateCd, "Mercury and Air Toxics Emissions (MATS)", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/state/mats-hourly-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
          }
        }
        
        for(int row = 0; row < rowsPerQuarter.Count; row++){
          int year = Convert.ToInt32(rowsPerQuarter[row][0]);
          int quarter = Convert.ToInt32(rowsPerQuarter[row][1]);

          string startDate = Convert.ToString(rowsPerQuarter[row][2]);
          string endDate = Convert.ToString(rowsPerQuarter[row][3]);

          string urlParams = "beginDate=" + startDate.ToString() + "&endDate=" + endDate.ToString();

          _logger.LogInformation("Creating records for quarterly emissions. Year: {Year}, Quarter: {Quarter}", year, quarter);

          await _dbContext.CreateBulkFileRecord("Hourly-Apportioned-Emissions-Q"+quarter+"-"+year, job_id,year, quarter, null, "Emissions", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/hourly?" + urlParams, "emissions/hourly/quarter/emissions-hourly-" + year + "-q" + quarter + ".csv", job_id, null);
          await _dbContext.CreateBulkFileRecord("Daily-Apportioned-Emissions-Q"+quarter+"-"+year, job_id,year, quarter, null, "Emissions", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/daily?" + urlParams, "emissions/daily/quarter/emissions-daily-" + year + "-q" + quarter + ".csv", job_id, null);
          if(year >= 2015){ // MATS data started in 2015
            _logger.LogInformation("Creating records for Hourly-MATS-Q for year > 2015. Year: {Year}, Quarter: {Quarter}", year, quarter);
            await _dbContext.CreateBulkFileRecord("Hourly-MATS-Q"+quarter+"-"+year,job_id,year, quarter, null, "Mercury and Air Toxics Emissions (MATS)", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/quarter/mats-hourly-" + year + "-q" + quarter + ".csv", job_id, null);
          }
        }

        _logger.LogInformation("Executing procedure_set_dm_emissions_user");
        _dbContext.ExecuteSql("CALL camdaux.procedure_set_dm_emissions_user();");

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();

        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Executed ApportionedEmissionsBulkDataFiles job successfully. JobId: {JobId}", job_id);
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing ApportionedEmissionsBulkDataFiles job. JobId: {JobId}", job_id);
      }
    }
  }
}
