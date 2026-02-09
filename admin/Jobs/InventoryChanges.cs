using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class InventoryChanges : IJob, IJobMetadata<InventoryChanges>
  {
    public static string JobName => "Inventory Changes";
    public static string JobDescription => "Operates on an interval to determine if any remote facility/unit inventory changes require changes to existing monitoring plans.";
    public static string JobGroup => Constants.QuartzGroups.MAINTAINANCE;
    public static string TriggerName => "Inventory Changes Trigger";
    public static string TriggerDescription => "Operate every 5 minutes to determine if there are any new changes recorded in the inventory status log.";

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<InventoryChanges> _logger;
    private IConfiguration _configuration { get; }

    public InventoryChanges(NpgSqlContext dbContext, IConfiguration configuration, ILogger<InventoryChanges> logger)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Starting Inventory Changes job. Job ID: {JobId}", job_id);

      try
      {
        if (IsJobInProgress(context))
        {
          _logger.LogInformation("Inventory Changes job is already in progress, skipping execution. Job ID: {JobId}", job_id);
          return;
        }

        // Get the last job log record for the job name "Inventory Changes".
        JobLog lastCompletedJobLog = _dbContext.JobLogs.FromSqlRaw(@"
                        SELECT * FROM camdaux.job_log
                        WHERE job_name = {0}
                        AND status_cd = 'COMPLETE'
                        ORDER BY start_date DESC
                        LIMIT 1", context.JobDetail.Key.Name
                ).FirstOrDefault();

        JobLog jl = new JobLog();
        try
        {
          jl.JobId = job_id;
          jl.JobSystem = "Quartz";
          jl.JobClass = "Maintainance";
          jl.JobName = context.JobDetail.Key.Name;
          jl.AddDate = Utils.getCurrentEasternTime();
          jl.StartDate = Utils.getCurrentEasternTime();
          jl.EndDate = null;
          jl.StatusCd = "WIP";
          _dbContext.JobLogs.Add(jl);
          await _dbContext.SaveChangesAsync();

          // Get the last processed inventory status log ID from the additional details of the last job log.
          int lastProcessedInventoryStatusLogId = 0;
          if (lastCompletedJobLog != null && lastCompletedJobLog.AdditionalDetails != null)
          {
            lastProcessedInventoryStatusLogId = JsonConvert.DeserializeObject<InventoryChangesJobLogAdditionalDetails>(lastCompletedJobLog.AdditionalDetails).LastProcessedInventoryStatusLogId;
          }
          // Initialize the additional details of the job log with the last processed inventory status log ID.
          jl.AdditionalDetails = JsonConvert.SerializeObject(new InventoryChangesJobLogAdditionalDetails { LastProcessedInventoryStatusLogId = lastProcessedInventoryStatusLogId });

          // Retrieve all inventory status log records after the last processed log and with a data type code of eith `INVENTORY` or `UNIT_PROGRAM`.
          List<InventoryStatusLog> inventoryStatusLogs = _dbContext.InventoryStatusLogs.FromSqlRaw(@"
                        SELECT * FROM camdaux.inventory_status_log
                        WHERE inventory_status_log_id > {0}
                        AND data_type_cd IN ('INVENTORY', 'UNIT_PROGRAM')
                        ORDER BY inventory_status_log_id", lastProcessedInventoryStatusLogId
              ).ToList();

          // Call the stored procedure `camdecmpswks.update_mp_eval_status_and_reporting_freq` for each log record.
          // This is done sequentially to ensure that the records are processed in the same order that they were retrieved.
          using (var connection = new NpgsqlConnection(_dbContext.Database.GetConnectionString()))
          {
            connection.Open();

            using (var sqlTransaction = connection.BeginTransaction())
            {
              try
              {
                foreach (InventoryStatusLog inventoryStatusLog in inventoryStatusLogs)
                {
                  UpdateMpEvalStatusAndReportingFreq(inventoryStatusLog, connection, sqlTransaction);
                  // Update the additional details table with the ID of the last processed inventory status log.
                  jl.AdditionalDetails = JsonConvert.SerializeObject(new InventoryChangesJobLogAdditionalDetails { LastProcessedInventoryStatusLogId = inventoryStatusLog.InventoryStatusLogId });
                }
                sqlTransaction.Commit();
              }
              catch (Exception)
              {
                sqlTransaction.Rollback();
                throw;
              }
            }
          }

          jl.StatusCd = "COMPLETE";
          jl.EndDate = Utils.getCurrentEasternTime();
          _dbContext.JobLogs.Update(jl);
          await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
          jl.StatusCd = "ERROR";
          jl.EndDate = Utils.getCurrentEasternTime();
          jl.AdditionalDetails = e.Message;
          _dbContext.JobLogs.Update(jl);
          await _dbContext.SaveChangesAsync();
          throw;
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e, "An error occurred during the Inventory Changes job. Job ID: {JobId}", job_id);
      }

      _logger.LogInformation("Completed Inventory Changes job successfully. Job ID: {JobId}", job_id);
    }

    private bool IsJobInProgress(IJobExecutionContext context)
    {
      // Get the last job log record for the job name "Inventory Changes".
      JobLog lastJobLog = _dbContext.JobLogs.FromSqlRaw(@"
                        SELECT * FROM camdaux.job_log
                        WHERE job_name = '{0}'
                        ORDER BY start_date DESC
                        LIMIT 1", context.JobDetail.Key.Name
              ).FirstOrDefault();

      // Check the status of the last job log. If the status is "WIP", then return.
      if (lastJobLog != null && lastJobLog.StatusCd == "WIP")
      {
        return true;
      }
      return false;
    }

    private void UpdateMpEvalStatusAndReportingFreq(InventoryStatusLog inventoryStatusLog, NpgsqlConnection connection, NpgsqlTransaction sqlTransaction)
    {
      var parameters = new List<NpgsqlParameter>
        {
          new NpgsqlParameter("par_V_UNIT_ID", NpgsqlDbType.Numeric) { Value = inventoryStatusLog.UnitId, Direction = ParameterDirection.Input },
          new NpgsqlParameter("par_V_DATA_TYPE_CD", NpgsqlDbType.Varchar) { Value = inventoryStatusLog.DataTypeCd, Direction = ParameterDirection.Input }
        };
      _dbContext.ExecuteProcedure("camdecmpswks.update_mp_eval_status_and_reporting_freq", parameters, connection, sqlTransaction);
    }
  }
}
