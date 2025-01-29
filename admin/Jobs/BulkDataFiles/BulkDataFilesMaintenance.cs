using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFileMaintenance : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public BulkDataFileMaintenance(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing BulkDataFileMaintenance job");

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

        foreach (BulkFileQueue record in toDelete)
        {
            if(await context.Scheduler.CheckExists(new JobKey(record.JobId.ToString()))){
              await context.Scheduler.DeleteJob(new JobKey(record.JobId.ToString()));
            }
        }

        _dbContext.ExecuteSql("DELETE from camdaux.bulk_file_queue where add_date < now() - interval '30 days'");
        _dbContext.ExecuteSql("DELETE from camdaux.job_log where add_date < now() - interval '90 days'");

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
        LogHelper.error(e.Message);
      }

      LogHelper.info("Executed BulkDataFileMaintenance job");
    }
  }
}
