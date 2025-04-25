using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class FacilityAttributesBulkDataFiles : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<FacilityAttributesBulkDataFiles> _logger;

    public FacilityAttributesBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration, ILogger<FacilityAttributesBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing FacilityAttributesBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Facility Attributes' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        return; // Job already exists , do not run again
      }
      
      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        // Does data mart nightly exists for current date and has it completed
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          return;
        }
      }

      _logger.LogInformation("Creating Allowance Holdings JobLog. JobId: {JobId}", job_id);

      JobLog jl = new JobLog(); 

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Facility Attributes";
        jl.AddDate = Utils.getCurrentEasternTime();;
        jl.StartDate = Utils.getCurrentEasternTime();;
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerState = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_annual_facility_bulk_files_to_generate", 1);
        
        for(int row = 0; row < rowsPerState.Count; row++){
          int year = Convert.ToInt32(rowsPerState[row][0]);
          DateTime currentDate = Utils.getCurrentEasternTime();

          await _dbContext.CreateBulkFileRecord("Facility-" + year, job_id, year, null, null, "Facility", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/facilities/attributes?year=" + year, "facility/facility" + "-" + year + ".csv", job_id, null);
        }

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("FacilityAttributesBulkDataFiles job completed successfully. JobId: {JobId}", job_id);
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();;
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing FacilityAttributesBulkDataFiles job. JobId: {JobId}", job_id);
      }
    }
  }
}
