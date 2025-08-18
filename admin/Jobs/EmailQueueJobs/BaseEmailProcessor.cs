using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs.EmailQueueJobs
{
    public abstract class BaseEmailProcessor
    {
        protected const string SubmissionReminderEmailTypeForRecipientApi = "SUBMISSIONREMINDER";
        protected const string WindowNotificationEmailTypeForRecipientApi = "WINDOWNOTIFICATION";
        
        public const string SubmissionReminderEmailTypeForDb = "submissionReminder";
        public const string WindowNotificationEmailTypeForDb = "windowNotification";

        private readonly NpgSqlContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly Guid _jobId;

        protected BaseEmailProcessor(NpgSqlContext dbContext, IConfiguration configuration, ILogger logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _jobId = Guid.NewGuid();
        }

        protected abstract string GetEmailTypeForRecipientApi();
        protected abstract string GetEmailTypeForDatabase();
        protected abstract string GetJobName();

        private async Task UpdateEmailProcessStatus(List<EmailToProcess> records, string status, string note = null)
        {
            DateTime currentTime = Utils.getCurrentEasternTime();
            
            foreach (EmailToProcess process in records)
            {
                process.StatusCode = status;
                
                // Set started_time when status is set to WIP
                if (status == "WIP")
                {
                    process.StartedTime = currentTime;
                }
                
                // Add note and note_time if provided (for errors/warnings)
                if (!string.IsNullOrEmpty(note))
                {
                    process.Note = note;
                    process.NoteTime = currentTime;
                }
                
                _dbContext.EmailToProcessQueue.Update(process);
            }
            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleEmailProcessFailure(List<EmailToProcess> records, string errorMessage)
        {
            DateTime currentTime = Utils.getCurrentEasternTime();
            int maxRetries = int.Parse(_configuration["EASEY_QUARTZ_SCHEDULER_EMAIL_PROCESS_MAX_RETRIES"] ?? "3");
            
            foreach (EmailToProcess process in records)
            {
                // Increment failure count
                process.FailureCount = (process.FailureCount ?? 0) + 1;
                process.Note = errorMessage;
                process.NoteTime = currentTime;
                
                // Set status based on failure count
                if (process.FailureCount >= maxRetries)
                {
                    process.StatusCode = "ERROR";
                    _logger.LogError("{JobName}: Email record failed {FailureCount} times, setting to ERROR. ToProcessId: {ToProcessId}, Last error: {Error}", 
                        GetJobName(), process.FailureCount, process.ProcessId, errorMessage);
                }
                else
                {
                    process.StatusCode = "QUEUED"; // Retry - queued_time stays the same
                    _logger.LogWarning("{JobName}: Email record failed (attempt {FailureCount}/{MaxRetries}), requeuing for retry. ToProcessId: {ToProcessId}, Error: {Error}", 
                        GetJobName(), process.FailureCount, maxRetries, process.ProcessId, errorMessage);
                }
                
                _dbContext.EmailToProcessQueue.Update(process);
            }
            
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Main processing method that coordinates email grouping and sending
        /// Uses strategy pattern to group emails differently based on email type
        /// </summary>
        protected async Task ProcessEmailRecipients()
        {
            string jobName = GetJobName();
            _logger.LogInformation("{JobName}: Executing job. JobId: {JobId}", jobName, _jobId);

            JobLog jl = await CreateJobLogEntry(jobName);
            
            try
            {
                string notificationTypeForDb = GetEmailTypeForDatabase();
                
                // 1. Get all queued email_to_process records from database
                List<EmailToProcess> inQueue = _dbContext.EmailToProcessQueue.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.email_to_process
                    WHERE status_cd = 'QUEUED' AND email_type = {0}", notificationTypeForDb).ToList();

                if (inQueue.Count == 0)
                {
                    _logger.LogInformation("{JobName}: No queued emails found for type: {NotificationType}", jobName, notificationTypeForDb);
                    await CompleteJobLog(jl);
                    return;
                }

                _logger.LogInformation("{JobName}: Found {QueueCount} emails queued for processing", jobName, inQueue.Count);

                // 2: Mark all records as WIP
                await UpdateEmailProcessStatus(inQueue, "WIP");
                
                // 3: Collect unique facility IDs for CBS API call
                HashSet<long> plantIdSet = inQueue.Select(ep => Convert.ToInt64(ep.FacId)).ToHashSet();
                long[] plantIdList = plantIdSet.ToArray();
                
                _logger.LogInformation("{JobName}: Marked {QueueCount} email_to_process records as WIP, found {PlantCount} unique facilities",  jobName, inQueue.Count, plantIdSet.Count);

                // 4: Call camd-services to get all recipients for all facilities
                string notificationTypeForRecipientApi = GetEmailTypeForRecipientApi();
                RecipientResponse recipientResponse = await CallRecipientApi(notificationTypeForRecipientApi, plantIdList);

                if (recipientResponse.hasError && (recipientResponse.recipients == null || recipientResponse.recipients.Length == 0))
                {
                    string errorMessage = $"Recipient API failed: {recipientResponse.errorMessage ?? "Unknown error"}";
                    _logger.LogError("{JobName}: {ErrorMessage}. Handling failure for {RecordCount} records.", 
                        jobName, errorMessage, inQueue.Count);
                    await HandleEmailProcessFailure(inQueue, errorMessage);
                    await CompleteJobLog(jl);
                    return;
                }

                // 5: Apply grouping strategy based on email type
                // - Submission reminders: Group by recipient (1 email per person, for one or more relevant facilities)
                // - Window notifications: Group by facility (1 email per facility, for one or more relevant recipients)
                var groupingStrategy = EmailGroupingStrategyFactory.CreateStrategy(notificationTypeForDb);
                var emailGroups = groupingStrategy.GroupEmailRecords(inQueue, recipientResponse, _logger);
                
                if (emailGroups.Count == 0)
                {
                    string warningMessage = $"No email groups created for {plantIdSet.Count} facilities - API returned {recipientResponse.recipients?.Length ?? 0} recipients but none matched our facilities or had valid email addresses";
                    _logger.LogWarning("{JobName}: {Warning}. Handling failure for {RecordCount} records.",  jobName, warningMessage, inQueue.Count);
                    //If the recipient api returns no emails for all provided fac IDs, let's consider it 'unusual' and retry
                    await HandleEmailProcessFailure(inQueue, warningMessage);  
                    await CompleteJobLog(jl);
                    return;
                }

                // 6: Create email_to_send records based on groups
                int recordsCreated = await CreateGroupedEmailToSendRecords(emailGroups);
                int totalGroups = emailGroups.Count;
                _logger.LogInformation("{JobName}: Completed successfully. Created {RecordCount} records for {GroupCount} groups", jobName, recordsCreated, totalGroups);

                await CompleteJobLog(jl);
            }
            catch (Exception e)
            {
                await ErrorJobLog(jl, e);
                throw;
            }
        }

        private async Task<JobLog> CreateJobLogEntry(string jobName)
        {
            JobLog jl = new JobLog()
            {
                JobId = _jobId,
                JobSystem = "Quartz",
                JobClass = jobName,
                JobName = jobName,
                AddDate = Utils.getCurrentEasternTime(),
                StartDate = Utils.getCurrentEasternTime(),
                EndDate = null,
                StatusCd = "WIP"
            };
            _dbContext.JobLogs.Add(jl);
            await _dbContext.SaveChangesAsync();
            return jl;
        }

        private async Task CompleteJobLog(JobLog jl)
        {
            jl.StatusCd = "COMPLETE";
            jl.EndDate = Utils.getCurrentEasternTime();
            _dbContext.JobLogs.Update(jl);
            await _dbContext.SaveChangesAsync();
        }

        private async Task ErrorJobLog(JobLog jl, Exception e)
        {
            jl.StatusCd = "ERROR";
            jl.EndDate = Utils.getCurrentEasternTime();
            jl.AdditionalDetails = e.Message;
            _dbContext.JobLogs.Update(jl);
            await _dbContext.SaveChangesAsync();
            _logger.LogError(e, "{JobName}: Error executing job. JobId: {JobId}", GetJobName(), _jobId);
        }

        private async Task<RecipientResponse> CallRecipientApi(string notificationType, long[] plantIdList)
        {
            // Fire API Call
            ReminderNotificationPayload payload = new ReminderNotificationPayload()
            {
                plantIdList = plantIdList,
                emailType = notificationType,
                isMats = null,
                plantId = null,
                submissionType = null,
                userId = null
            };

            HttpClient client = new HttpClient();
            StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("x-api-key", _configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
            client.DefaultRequestHeaders.Add("x-client-id", _configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);

            string clientToken = await Utils.generateClientToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

            _logger.LogInformation("{JobName}: Calling recipient API for {NotificationType} with {PlantCount} facilities", GetJobName(), notificationType, plantIdList.Length);

            HttpResponseMessage response = await client.PostAsync(_configuration["EASEY_CAMD_SERVICES"] + "/support/email/emailRecipientList", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("{JobName}: Recipient API returned HTTP {StatusCode}: {ReasonPhrase}", GetJobName(), response.StatusCode, response.ReasonPhrase ?? "No reason phrase");
                // Return empty recipient list on HTTP error, don't throw
                return new RecipientResponse { recipients = [], hasError = true, errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase ?? "No reason phrase"}" };
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            
            RecipientResponse recipientResponse = JsonConvert.DeserializeObject<RecipientResponse>(responseContent);

            // Check for API errors but don't throw - just return the error response
            if (recipientResponse.hasError)
            {
                _logger.LogError("{JobName}: Recipient List API returned error: {ErrorMessage}", GetJobName(), recipientResponse.errorMessage ?? "Unknown error");
            }

            // Log summary
            int recipientCount = recipientResponse.recipients?.Length ?? 0;
            _logger.LogInformation("{JobName}: Recipient API returned {RecipientCount} recipients for {NotificationType}", GetJobName(), recipientCount, notificationType);
            return recipientResponse;
        }


        /// <summary>
        /// Creates email_to_send records based on grouped emails
        /// Each group becomes a single email_to_send record with potentially multiple recipients
        /// </summary>
        private async Task<int> CreateGroupedEmailToSendRecords(List<EmailGroup> emailGroups)
        {
            int totalRecordsCreated = 0;
            int groupsWithErrors = 0;

            foreach (var group in emailGroups)
            {
                try
                {
                    // Combine all recipients with semicolon separator for TO field
                    // Example: "john@epa.gov;jane@epa.gov;bob@epa.gov"
                    string recipientList = string.Join(";", group.Recipients);
                    
                    EmailToSend emailToSend = new EmailToSend()
                    {
                        Context = group.CombinedContext,  // JSON array for reminders, single context for notifications
                        StatusCode = "QUEUED",
                        TemplateId = group.TemplateId,
                        ToEmail = recipientList,          // Multiple recipients in single field
                        FromEmail = _configuration["EASEY_QUARTZ_SCHEDULER_WINDOW_NOTIFICATION_FROM_EMAIL"]
                    };

                    _dbContext.EmailToSend.Add(emailToSend);
                    totalRecordsCreated++;

                    // Mark all source email_to_process records as COMPLETE
                    foreach (var emailRecord in group.EmailRecords)
                    {
                        emailRecord.StatusCode = "COMPLETE";
                        _dbContext.EmailToProcessQueue.Update(emailRecord);
                    }
                    
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    groupsWithErrors++;
                    string errorMessage = $"Failed to create EmailToSend record for group {group.GroupKey}: {ex.Message}";
                    string affectedIds = string.Join(", ", group.EmailRecords.Select(er => er.ProcessId));
                    _logger.LogError(ex, "{JobName}: {ErrorMessage}. Affected ToProcessIds: [{ToProcessIds}]", GetJobName(), errorMessage, affectedIds);
                    
                    // Clear tracking to avoid conflicts (prevent Entity Framework state conflicts when continuing processing after an exception)
                    _dbContext.ChangeTracker.Clear();
                    
                    // Handle failure for all records in this group
                    await HandleEmailProcessFailure(group.EmailRecords, errorMessage);
                }
            }
            
            if (groupsWithErrors > 0)
            {
                int groupCount = emailGroups.Count;
                _logger.LogWarning("{JobName}: Processed {GroupCount} groups, created {RecordCount} records. {ErrorCount} groups had errors", 
                    GetJobName(), groupCount, totalRecordsCreated, groupsWithErrors);
            }
            else
            {
                int groupCount = emailGroups.Count;
                _logger.LogInformation("{JobName}: Processed {GroupCount} groups, created {RecordCount} records", GetJobName(), groupCount, totalRecordsCreated);
            }

            return totalRecordsCreated;
        }
    }
}