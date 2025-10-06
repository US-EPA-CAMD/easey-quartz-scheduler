using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Epa.Camd.Quartz.Scheduler.Jobs.EmailQueueJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    [DisallowConcurrentExecution]
    public class SubmissionWindowManagement : IJob, IJobMetadata<SubmissionWindowManagement>
    {
        public static string JobName => "Submission Window Management";
        public static string JobDescription => "Manages emission submission windows";
        public static string JobGroup => Constants.QuartzGroups.MAINTAINANCE;
        public static string TriggerName => "Submission Window Management Trigger";
        public static string TriggerDescription => "Runs daily at 3AM to initialize and close emission submission access";

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
                    InitAndCloseEmSubmissionAccess();

                    // Trigger follow-up jobs
                    await TriggerFollowUpJobs(context);

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

        private void InitAndCloseEmSubmissionAccess()
        {
            _logger.LogInformation("Calling stored procedure camdecmpsaux.init_and_close_em_submission_access with date: {Date}", DateTime.Today.ToString("yyyy-MM-dd"));

            var parameters = new List<NpgsqlParameter>
            {
                _dbContext.CreateParameter("v_sysdate", DateTime.Today.ToString("yyyy-MM-dd"), NpgsqlDbType.Text, System.Data.ParameterDirection.Input),
                _dbContext.CreateParameter("v_fac_id", null, NpgsqlDbType.Numeric, System.Data.ParameterDirection.Input),
                _dbContext.CreateParameter("v_result", null, NpgsqlDbType.Text, System.Data.ParameterDirection.InputOutput),
                _dbContext.CreateParameter("v_error_msg", null, NpgsqlDbType.Text, System.Data.ParameterDirection.InputOutput)
            };

            var command = _dbContext.ExecuteProcedure("camdecmpsaux.init_and_close_em_submission_access", parameters);

            // Get the output values
            var resultParam = command.Parameters["v_result"];
            var errorMsgParam = command.Parameters["v_error_msg"];

            var result = resultParam.Value != DBNull.Value ? resultParam.Value.ToString() : null;
            var errorMsg = errorMsgParam.Value != DBNull.Value ? errorMsgParam.Value.ToString() : null;

            // Get input parameters for error logging
            var sysdateParam = command.Parameters["v_sysdate"];
            var facIdParam = command.Parameters["v_fac_id"];
            var sysdate = sysdateParam.Value != DBNull.Value ? sysdateParam.Value.ToString() : "null";
            var facId = facIdParam.Value != DBNull.Value ? facIdParam.Value.ToString() : "null";

            // Always log the result value, including when it's null
            _logger.LogInformation("camdecmpsaux.init_and_close_em_submission_access completed with result: {Result}", result ?? "null");

            // Validate result and handle unexpected values
            if (result == "T")
            {
                _logger.LogInformation("camdecmpsaux.init_and_close_em_submission_access completed successfully");
            }
            else if (result == "F")
            {
                _logger.LogError("camdecmpsaux.init_and_close_em_submission_access returned error: {ErrorMsg}. Parameters: sysdate={Sysdate}, fac_id={FacId}", errorMsg ?? "null", sysdate, facId);
            }
            else
            {
                // Unexpected result value - treat as error
                _logger.LogError("camdecmpsaux.init_and_close_em_submission_access returned unexpected result value: {Result}. Expected 'T' or 'F'. Error message: {ErrorMsg}. Parameters: sysdate={Sysdate}, fac_id={FacId}", result ?? "null", errorMsg ?? "null", sysdate, facId);
                throw new Exception($"camdecmpsaux.init_and_close_em_submission_access returned unexpected result value: '{result ?? "null"}'. Expected 'T' or 'F'. Error message: {errorMsg ?? "null"}. Parameters: sysdate={sysdate}, fac_id={facId}");
            }
        }

        private async Task TriggerFollowUpJobs(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Triggering follow-up jobs: ProcessSubmissionReminders and ProcessWindowNotifications");

                // Trigger ProcessSubmissionReminders
                string reminderJobId = Guid.NewGuid().ToString();
                IJobDetail reminderJob = JobBuilder.Create<ProcessSubmissionReminders>()
                    .WithIdentity(new JobKey(reminderJobId, Epa.Camd.Quartz.Scheduler.Constants.QuartzGroups.MAINTAINANCE))
                    .WithDescription("Process submission reminder emails triggered by SubmissionWindowManagement")
                    .Build();

                ITrigger reminderTrigger = TriggerBuilder.Create()
                    .WithIdentity(new TriggerKey("ProcessSubmissionReminders-" + reminderJobId, Epa.Camd.Quartz.Scheduler.Constants.QuartzGroups.MAINTAINANCE))
                    .StartNow()
                    .Build();

                await context.Scheduler.ScheduleJob(reminderJob, reminderTrigger);
                _logger.LogInformation("Successfully scheduled ProcessSubmissionReminders job with ID: {JobId}", reminderJobId);

                // Trigger ProcessWindowNotifications
                string windowJobId = Guid.NewGuid().ToString();
                IJobDetail windowJob = JobBuilder.Create<ProcessWindowNotifications>()
                    .WithIdentity(new JobKey(windowJobId, Epa.Camd.Quartz.Scheduler.Constants.QuartzGroups.MAINTAINANCE))
                    .WithDescription("Process window notification emails triggered by SubmissionWindowManagement")
                    .Build();

                ITrigger windowTrigger = TriggerBuilder.Create()
                    .WithIdentity(new TriggerKey("ProcessWindowNotifications-" + windowJobId, Epa.Camd.Quartz.Scheduler.Constants.QuartzGroups.MAINTAINANCE))
                    .StartNow()
                    .Build();

                await context.Scheduler.ScheduleJob(windowJob, windowTrigger);
                _logger.LogInformation("Successfully scheduled ProcessWindowNotifications job with ID: {JobId}", windowJobId);

                _logger.LogInformation("All follow-up jobs triggered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering follow-up jobs");
                throw;
            }
        }
    }
}
