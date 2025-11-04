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
  public class AllowanceTransactionsBulkDataFiles : IJob, IJobMetadata<AllowanceTransactionsBulkDataFiles>
  {
    public static string JobName => "Allowance Transactions Bulk Data";
    public static string JobDescription => "Determine which allowance transactions need to be regenerated and schedule BulkDataFile jobs to handle the regen";
    public static string JobGroup => Constants.QuartzGroups.BULK_DATA;
    public static string TriggerName => "Allowance Transactions Bulk Data Trigger";
    public static string TriggerDescription => "Runs nightly to determine if files need to be regenerated based on query results";

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<AllowanceTransactionsBulkDataFiles> _logger;

    public AllowanceTransactionsBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration, ILogger<AllowanceTransactionsBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing AllowanceTransactionsBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Transactions' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        _logger.LogWarning("Job already exists for today. Skipping execution. JobId: {JobId}", job_id);
        return; // Job already exists , do not run again
      }

      // Does data mart nightly exists for current date and has it completed
      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          _logger.LogWarning("Datamart nightly job has not completed. Skipping execution. JobId: {JobId}", job_id);
          return;
        }
      }

      _logger.LogInformation("Creating Allowance Transactions JobLog. JobId: {JobId}", job_id);

      JobLog jl = new JobLog(); 

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Transactions";
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
          int year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "transactionBeginDate=1993-03-23&transactionEndDate=" + year + "-12-31&programCodeInfo=" + code;

          _logger.LogInformation("Creating bulk file record for program code: {Code}, Year: {Year}", code, year);
          await _dbContext.CreateBulkFileRecord("Allowance-Transactions-"+ code , job_id, year, null, null, "Allowance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-transactions?" + urlParams, "allowance/transactions-" + code.ToLower() + ".csv", job_id, code);
        }
        
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully completed AllowanceTransactionsBulkDataFiles job. JobId: {JobId}", job_id);
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing AllowanceTransactionsBulkDataFiles job. JobId: {JobId}", job_id);
      }
    }
  }
}
