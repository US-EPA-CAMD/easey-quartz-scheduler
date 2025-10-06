using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class AllowanceHoldingsBulkDataFiles : IJob, IJobMetadata<AllowanceHoldingsBulkDataFiles>
  {
    public static string JobName => "Allowance Holdings Bulk Data";
    public static string JobDescription => "Generate Allowance Holdings and schedule BulkDataFile jobs to handle the regen";
    public static string JobGroup => Constants.QuartzGroups.BULK_DATA;
    public static string TriggerName => "Allowance Holdings Bulk Data Trigger";
    public static string TriggerDescription => "Runs nightly to generate allowance holdings files";

    private Guid _jobId = Guid.NewGuid();
    private NpgSqlContext _dbContext = null;
    private readonly ILogger<AllowanceHoldingsBulkDataFiles> _logger;

    public AllowanceHoldingsBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration, ILogger<AllowanceHoldingsBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing AllowanceHoldingsBulkDataFiles job. JobId: {JobId}", _jobId);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Holdings' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        _logger.LogInformation("Job already exists for today. Skipping execution. JobId: {JobId}", _jobId);
        return; // Job already exists , do not run again
      }
      
      // Does data mart nightly exists for current date and has it completed
      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          _logger.LogInformation("Datamart nightly job has not completed. Skipping execution. JobId: {JobId}", _jobId);
          return;
        }
      }

      _logger.LogInformation("Creating Allowance Holdings JobLog. JobId: {JobId}", _jobId);

      JobLog jl = new JobLog(); 

      try
      {

        jl.JobId = _jobId;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Holdings";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> programCodeRows = await this._dbContext.ExecuteSqlQuery("SELECT prg_cd FROM camdmd.program_code pc WHERE pc.bulk_file_active = 1", 1);
        string[] programCodes = new string[programCodeRows.Count];

        for(int i = 0; i < programCodeRows.Count; i++){
          programCodes[i] = (string) programCodeRows[i][0];
        }
        
        for(int row = 0; row < programCodes.Length; row++){
          string code = programCodes[row];
          decimal year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "programCodeInfo=" + code;

          _logger.LogInformation("Creating bulk file record for programCode: {ProgramCode}", code);
          await _dbContext.CreateBulkFileRecord("Allowance-Holdings-" + code, _jobId ,null, null, null, "Allowance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-holdings?" + urlParams, "allowance/holdings-" + code.ToLower() + ".csv", _jobId, code);
        }
        
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("{JobName} job completed successfully. JobId: {JobId}", nameof(AllowanceHoldingsBulkDataFiles), _jobId);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Error executing {JobName}. JobId: {JobId}, Error: {ErrorMessage}", nameof(AllowanceHoldingsBulkDataFiles),
                _jobId,
                e.Message ?? "No message");

        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
      }
    }
  }
}
