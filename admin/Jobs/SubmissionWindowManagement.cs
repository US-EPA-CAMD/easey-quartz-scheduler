using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class SubmissionWindowManagement : IJob
    {
        private readonly NpgSqlContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubmissionWindowManagement> _logger;

        public SubmissionWindowManagement(NpgSqlContext dbContext, IConfiguration configuration, ILogger<SubmissionWindowManagement> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Guid job_id = Guid.NewGuid();
            
            try
            {
                _logger.LogInformation("SubmissionWindowManagement job started. Job ID: {JobId}", job_id);

                // Check if job is already running
                if (IsJobInProgress(context))
                {
                    _logger.LogInformation("SubmissionWindowManagement job is already in progress. Skipping execution. Job ID: {JobId}", job_id);
                    return;
                }

                // Create JobLog record
                JobLog jl = new JobLog()
                {
                    JobId = job_id,
                    JobSystem = "Quartz",
                    JobClass = "SubmissionWindowManagement", 
                    JobName = context.JobDetail.Key.Name,
                    StartDate = Utils.getCurrentEasternTime(),
                    StatusCd = "WIP"
                };
                _dbContext.JobLogs.Add(jl);
                await _dbContext.SaveChangesAsync();

                try
                {
                    // Call the stored procedure to do the actual management of submission window
                    await CallInitAndCloseEmSubmissionAccess();

                    // Mark job as completed
                    jl.StatusCd = "COMPLETE";
                    jl.EndDate = Utils.getCurrentEasternTime();
                    _dbContext.JobLogs.Update(jl);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("SubmissionWindowManagement job completed successfully. Job ID: {JobId}", job_id);
                }
                catch (Exception ex)
                {
                    // Mark job as error
                    jl.StatusCd = "ERROR";
                    jl.EndDate = Utils.getCurrentEasternTime();
                    jl.AdditionalDetails = ex.Message;
                    _dbContext.JobLogs.Update(jl);
                    await _dbContext.SaveChangesAsync();
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred during the SubmissionWindowManagement job. Job ID: {JobId}", job_id);
                throw;
            }
        }

        private bool IsJobInProgress(IJobExecutionContext context)
        {
            // Get the last job log record for the job name
            JobLog lastJobLog = _dbContext.JobLogs.FromSqlRaw(@"
                        SELECT * FROM camdaux.job_log
                        WHERE job_name = '{0}'
                        ORDER BY start_date DESC
                        LIMIT 1", context.JobDetail.Key.Name
                    ).FirstOrDefault();

            // Check the status of the last job log. If the status is "WIP", then return true.
            if (lastJobLog != null && lastJobLog.StatusCd == "WIP")
            {
                return true;
            }
            return false;
        }

        private async Task CallInitAndCloseEmSubmissionAccess()
        {
            _logger.LogInformation("Calling stored procedure camdecmpsaux.init_and_close_em_submission_access with date: {Date}", DateTime.Today.ToString("yyyy-MM-dd"));

            var parameters = new List<NpgsqlParameter>
            {
                _dbContext.CreateParameter("par_v_sysdate", DateTime.Today.ToString("yyyy-MM-dd"), NpgsqlDbType.Date, System.Data.ParameterDirection.Input),
                _dbContext.CreateParameter("par_v_fac_id", null, NpgsqlDbType.Numeric, System.Data.ParameterDirection.Input),
                _dbContext.CreateParameter("par_v_result", null, NpgsqlDbType.Text, System.Data.ParameterDirection.InputOutput),
                _dbContext.CreateParameter("par_v_error_msg", null, NpgsqlDbType.Text, System.Data.ParameterDirection.InputOutput)
            };

            var command = _dbContext.ExecuteProcedure("camdecmpsaux.init_and_close_em_submission_access", parameters);

            // Get the output values
            var resultParam = command.Parameters["par_v_result"];
            var errorMsgParam = command.Parameters["par_v_error_msg"];

            var result = resultParam.Value != DBNull.Value ? resultParam.Value.ToString() : null;
            var errorMsg = errorMsgParam.Value != DBNull.Value ? errorMsgParam.Value.ToString() : null;

            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogInformation("camdecmpsaux.init_and_close_em_submission_access completed with result: {Result}", result);
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                _logger.LogError("camdecmpsaux.init_and_close_em_submission_access returned error: {ErrorMsg}", errorMsg);
                throw new Exception("camdecmpsaux.init_and_close_em_submission_access returned error: " + errorMsg);
            }

            _logger.LogInformation("camdecmpsaux.init_and_close_em_submission_access completed successfully");
        }
    }
}