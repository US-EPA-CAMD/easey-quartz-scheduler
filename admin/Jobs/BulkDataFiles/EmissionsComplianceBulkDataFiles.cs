using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class EmissionsComplianceBulkDataFiles  : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<EmissionsComplianceBulkDataFiles> _logger;

    public EmissionsComplianceBulkDataFiles (NpgSqlContext dbContext, IConfiguration configuration, ILogger<EmissionsComplianceBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing EmissionsComplianceBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Emissions Compliance' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        _logger.LogWarning("EmissionsCompliance job already exists for today. Skipping. JobId: {JobId}", job_id);
        return; // Job already exists , do not run again
      }

      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        // Does data mart nightly exists for current date and has it completed
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          _logger.LogWarning("Datamart Nightly job has not completed. Skipping EmissionsCompliance job. JobId: {JobId}", job_id);
          return;
        }
      }

      _logger.LogInformation("Creating Emissions Compliance JobLog. JobId: {JobId}", job_id);

      JobLog jl = new JobLog(); 

      try
      {
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Emissions Compliance";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerPrg = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_emissions_based_compliance_bulk_files_to_generate", 2);

        if(rowsPerPrg.Count > 0){
          _logger.LogInformation("Generating compliance bulk file record for ARPNOX. JobId: {JobId}", job_id);
          await this._dbContext.CreateBulkFileRecord("Emissions-Compliance-ARPNOX",job_id,null, null, null, "Compliance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions-compliance", "compliance/compliance-arpnox.csv", job_id, "ARP");
        }
                
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Executed EmissionsComplianceBulkDataFiles job successfully. JobId: {JobId}", job_id);
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing EmissionsComplianceBulkDataFiles job. JobId: {JobId}", job_id);
      }
    }
  }
}
