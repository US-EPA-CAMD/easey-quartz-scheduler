using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class BulkDataFileMaintenance : IJob, IJobMetadata<BulkDataFileMaintenance>
  {
    public static string JobName => "Bulk Data File Maintenance";
    public static string JobDescription => "Run a check on the bulk data file maintenance queue";
    public static string JobGroup => Constants.QuartzGroups.BULK_DATA;
    public static string TriggerName => "Bulk Data File Maintenance Trigger";
    public static string TriggerDescription => "Run nightly and check which files need to get rerun or cleaned up";

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<BulkDataFileMaintenance> _logger;

    public BulkDataFileMaintenance(NpgSqlContext dbContext, IConfiguration configuration, ILogger<BulkDataFileMaintenance> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing BulkDataFileMaintenance job. JobId: {JobId}", job_id);

      JobLog jl = new JobLog();
      try{
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Bulk Data File Maintenance";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";
        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();

        List<BulkFileQueue> toDelete = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd IN('QUEUED', 'ERROR', 'WIP') AND add_date < now() - interval '30 days'"
          ).ToList();

        _logger.LogInformation("Found {Count} stale job(s) to remove from scheduler", toDelete?.Count ?? 0);

        foreach (BulkFileQueue record in toDelete)
        {
            if(await context.Scheduler.CheckExists(new JobKey(record.JobId.ToString()))){
              _logger.LogInformation("Deleting stale job from scheduler. JobId: {JobId}", record.JobId);
              await context.Scheduler.DeleteJob(new JobKey(record.JobId.ToString()));
            }
        }

        _logger.LogInformation("Cleaning up stale records in bulk_file_queue and job_log tables");
        _dbContext.ExecuteSql("DELETE from camdaux.bulk_file_queue where add_date < now() - interval '30 days'");
        _dbContext.ExecuteSql("DELETE from camdaux.job_log where add_date < now() - interval '90 days'");

        _logger.LogInformation("Calling camdaux.procedure_bulk_file_requeue_check()");
        _dbContext.ExecuteSql("CALL camdaux.procedure_bulk_file_requeue_check();");

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();

      }catch(Exception e){
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing BulkDataFileMaintenance job. JobId: {JobId}", job_id);
      }

      _logger.LogInformation("Executed BulkDataFileMaintenance job. JobId: {JobId}", job_id);
    }
  }
}
