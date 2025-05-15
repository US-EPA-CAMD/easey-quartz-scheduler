using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class AllowanceComplianceBulkDataFiles  : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<AllowanceComplianceBulkDataFiles> _logger;

    public AllowanceComplianceBulkDataFiles (NpgSqlContext dbContext, IConfiguration configuration, ILogger<AllowanceComplianceBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing AllowanceComplianceBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Compliance' AND add_date::date = now()::date;", 9);
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

      _logger.LogInformation("Creating Allowance Compliance JobLog. JobId: {JobId}", job_id);

      JobLog jl = new JobLog();
      List<string> generatedPrograms = new List<string>();

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Compliance";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();

        List<List<Object>> distinctPrograms = await _dbContext.ExecuteSqlQuery("SELECT DISTINCT prg_code FROM camdaux.vw_allowance_based_compliance_bulk_files_to_generate", 1);


        foreach(var programRow in distinctPrograms){
          string code = (string)programRow[0];
          string urlParams = "programCodeInfo=" + code;

          await _dbContext.CreateBulkFileRecord("Allowance-Compliance-" + code, job_id, null, null, null, "Compliance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-compliance?" + urlParams, "compliance/compliance-" + code.ToLower() + ".csv", job_id, code);
          generatedPrograms.Add(code);
          _logger.LogInformation("Generated allowance compliance file program {ProgramCode}. JobId: {JobId}", code, job_id);
        }
        
                
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = $"Generated programs: {string.Join(", ", generatedPrograms)}";
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("AllowanceComplianceBulkDataFiles job completed successfully. Generated files {ProgramCount} programs: {ProgramList}. JobId: {JobId}", generatedPrograms.Count, string.Join(", ", generatedPrograms), job_id);
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing AllowanceComplianceBulkDataFiles job. JobId: {JobId}", job_id);
      }
    }
  }
}
