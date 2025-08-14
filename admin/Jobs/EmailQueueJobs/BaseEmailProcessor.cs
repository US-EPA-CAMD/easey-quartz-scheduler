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

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public abstract class BaseEmailProcessor
    {
        public const string SUBMISSION_REMINDER_EMAIL_TYPE_FOR_RECIP_API = "SUBMISSIONREMINDER";
        public const string WINDOW_NOTIFICATION_EMAIL_TYPE_FOR_RECIP_API = "WINDOWNOTIFICATION";
        
        public const string SUBMISSION_REMINDER_EMAIL_TYPE_FOR_DB = "submissionReminder";
        public const string WINDOW_NOTIFICATION_EMAIL_TYPE_FOR_DB = "submissionWindow";

        protected readonly NpgSqlContext _dbContext;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;
        protected readonly Guid _jobId;

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

        private async Task UpdateEmailProcessStatus(List<EmailToProcess> records, string status)
        {
            foreach (EmailToProcess process in records)
            {
                process.StatusCode = status;
                _dbContext.EmailToProcessQueue.Update(process);
            }
            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateSingleEmailProcessStatus(EmailToProcess record, string status)
        {
            record.StatusCode = status;
            _dbContext.EmailToProcessQueue.Update(record);
            await _dbContext.SaveChangesAsync();
        }

        protected async Task<Dictionary<decimal, HashSet<string>>> ProcessEmailRecipients()
        {
            string jobName = GetJobName();
            _logger.LogInformation("{JobName}: Executing job. JobId: {JobId}", jobName, _jobId);

            JobLog jl = await CreateJobLogEntry(jobName);
            
            try
            {
                string notificationTypeForDb = GetEmailTypeForDatabase();
                
                // Get queued emails for this type
                List<EmailToProcess> inQueue = _dbContext.EmailToProcessQueue.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.email_to_process
                    WHERE status_cd = 'QUEUED' AND email_type = {0}", notificationTypeForDb).ToList();

                if (inQueue.Count == 0)
                {
                    _logger.LogInformation("{JobName}: No queued emails found for type: {NotificationType}", jobName, notificationTypeForDb);
                    await CompleteJobLog(jl);
                    return new Dictionary<decimal, HashSet<string>>();
                }

                _logger.LogInformation("{JobName}: Found {QueueCount} emails queued for processing", jobName, inQueue.Count);

                // Mark records as WIP and collect plant IDs
                await UpdateEmailProcessStatus(inQueue, "WIP");
                
                HashSet<long> plantIdSet = new HashSet<long>();
                foreach (EmailToProcess process in inQueue)
                {
                    long plantId = Convert.ToInt64(process.FacId);
                    plantIdSet.Add(plantId);
                    _logger.LogInformation("{JobName}: Converting EmailToProcess FacId {FacId} (decimal) to plantId {PlantId} (long)", 
                        jobName, process.FacId, plantId);
                }
                _logger.LogInformation("{JobName}: Marked {QueueCount} email records as WIP, found {PlantCount} unique facilities. PlantIds: [{PlantIds}]", 
                    jobName, inQueue.Count, plantIdSet.Count, string.Join(", ", plantIdSet.OrderBy(x => x)));

                // Create plant ID list for API call
                long[] plantIdList = new long[plantIdSet.Count];
                plantIdSet.CopyTo(plantIdList);

                // Call recipient API
                string notificationTypeForRecipientApi = GetEmailTypeForRecipientApi();
                RecipientResponse recipientResponse = await CallRecipientAPI(notificationTypeForRecipientApi, plantIdList);

                // If recipient API failed completely, revert all records to QUEUED
                if (recipientResponse.hasError && (recipientResponse.recipients == null || recipientResponse.recipients.Length == 0))
                {
                    _logger.LogError("{JobName}: Recipient API failed completely: {ErrorMessage}. Reverting {RecordCount} records to QUEUED.", jobName, recipientResponse.errorMessage ?? "Unknown error", inQueue.Count);
                    await UpdateEmailProcessStatus(inQueue, "QUEUED");
                    await CompleteJobLog(jl);
                    return new Dictionary<decimal, HashSet<string>>();
                }

                // Process response and build facility-to-emails mapping
                Dictionary<decimal, HashSet<string>> facIdToEmails = BuildFacilityEmailMapping(recipientResponse);

                // Create EmailToSend records
                int emailsCreated = await CreateEmailToSendRecords(inQueue, facIdToEmails);
                _logger.LogInformation("{JobName}: Completed successfully. Created {EmailCount} emails for {FacilityCount} facilities", jobName, emailsCreated, facIdToEmails.Count);

                await CompleteJobLog(jl);
                return facIdToEmails;
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

        private async Task<RecipientResponse> CallRecipientAPI(string notificationType, long[] plantIdList)
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
                return new RecipientResponse { recipients = new Recipient[0], hasError = true, errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase ?? "No reason phrase"}" };
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

        private Dictionary<decimal, HashSet<string>> BuildFacilityEmailMapping(RecipientResponse recipientResponse)
        {
            Dictionary<decimal, HashSet<string>> facIdToEmails = new Dictionary<decimal, HashSet<string>>();

            if (recipientResponse.recipients == null || recipientResponse.recipients.Length == 0)
            {
                _logger.LogWarning("{JobName}: No recipients found in API response", GetJobName());
                return facIdToEmails;
            }

            _logger.LogInformation("{JobName}: Processing {RecipientCount} recipients from API response", GetJobName(), recipientResponse.recipients.Length);

            foreach (Recipient r in recipientResponse.recipients)
            {
                // Parse the single email address (may have display name format)
                string emailAddress = r.emailAddressList;
                
                if (string.IsNullOrEmpty(emailAddress))
                {
                    _logger.LogWarning("{JobName}: Empty email address found in recipient response", GetJobName());
                    continue;
                }

                _logger.LogInformation("{JobName}: Processing recipient with email '{Email}' for plant IDs: [{PlantIds}]", 
                    GetJobName(), emailAddress, string.Join(", ", r.plantIdList));

                foreach (long facId in r.plantIdList)
                {
                    decimal facIdDecimal = Convert.ToDecimal(facId);
                    
                    _logger.LogInformation("{JobName}: Converting plantId {PlantId} (long) to facIdDecimal {FacIdDecimal} (decimal)", 
                        GetJobName(), facId, facIdDecimal);
                    
                    if (facIdToEmails.ContainsKey(facIdDecimal))
                    {
                        facIdToEmails[facIdDecimal].Add(emailAddress);
                        _logger.LogInformation("{JobName}: Added email to existing facId {FacId}, total emails: {EmailCount}", 
                            GetJobName(), facIdDecimal, facIdToEmails[facIdDecimal].Count);
                    }
                    else
                    {
                        HashSet<string> emails = new HashSet<string> { emailAddress };
                        facIdToEmails.Add(facIdDecimal, emails);
                        _logger.LogInformation("{JobName}: Created new mapping for facId {FacId} with 1 email", GetJobName(), facIdDecimal);
                    }
                }
            }

            _logger.LogInformation("{JobName}: Built email mapping for {FacilityCount} facilities. Keys: [{Keys}]", 
                GetJobName(), facIdToEmails.Count, string.Join(", ", facIdToEmails.Keys));
            return facIdToEmails;
        }

        private async Task<int> CreateEmailToSendRecords(List<EmailToProcess> inQueue, Dictionary<decimal, HashSet<string>> facIdToEmails)
        {
            int totalEmailsCreated = 0;
            int facilitiesWithoutRecipients = 0;
            int facilitiesWithErrors = 0;

            _logger.LogInformation("{JobName}: Processing {QueueCount} EmailToProcess records against {MappingCount} facility mappings", 
                GetJobName(), inQueue.Count, facIdToEmails.Count);

            foreach (EmailToProcess emailToProcess in inQueue)
            {
                _logger.LogInformation("{JobName}: Processing EmailToProcess with FacId: {FacId} (type: {FacIdType})", 
                    GetJobName(), emailToProcess.FacId, emailToProcess.FacId.GetType().Name);

                HashSet<string> allEmailsForFacility = new HashSet<string>();

                // Add recipients from API response
                if (facIdToEmails.ContainsKey(emailToProcess.FacId))
                {
                    _logger.LogInformation("{JobName}: Found mapping for FacId {FacId}, adding {EmailCount} emails", 
                        GetJobName(), emailToProcess.FacId, facIdToEmails[emailToProcess.FacId].Count);
                    
                    foreach (string email in facIdToEmails[emailToProcess.FacId])
                    {
                        allEmailsForFacility.Add(email);
                        _logger.LogInformation("{JobName}: Added email: {Email}", GetJobName(), email);
                    }
                }
                else
                {
                    _logger.LogWarning("{JobName}: NO MAPPING FOUND for FacId {FacId}. Available keys: [{AvailableKeys}]", 
                        GetJobName(), emailToProcess.FacId, string.Join(", ", facIdToEmails.Keys));
                }

                _logger.LogInformation("{JobName}: Total emails found for FacId {FacId}: {EmailCount}", 
                    GetJobName(), emailToProcess.FacId, allEmailsForFacility.Count);

                if (allEmailsForFacility.Count > 0)
                {
                    try
                    {
                        // Create EmailToSend records for this facility
                        foreach (string emailTo in allEmailsForFacility)
                        {
                            EmailToSend emailToSend = new EmailToSend()
                            {
                                Context = emailToProcess.Context,
                                StatusCode = "QUEUED",
                                TemplateId = emailToProcess.EventCode,
                                ToEmail = emailTo,
                                FromEmail = _configuration["EASEY_QUARTZ_SCHEDULER_WINDOW_NOTIFICATION_FROM_EMAIL"]
                            };

                            _dbContext.EmailToSend.Add(emailToSend);
                            totalEmailsCreated++;
                        }
                        
                        // Mark as COMPLETE only when EMAIL_TO_SEND records are successfully created
                        emailToProcess.StatusCode = "COMPLETE";
                        _dbContext.EmailToProcessQueue.Update(emailToProcess);
                        
                        // Save changes for this facility immediately
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("{JobName}: Successfully created emails and marked FacId {FacId} as COMPLETE", GetJobName(), emailToProcess.FacId);
                    }
                    catch (Exception ex)
                    {
                        facilitiesWithErrors++;
                        _logger.LogError(ex, "{JobName}: Failed to create EmailToSend records for FacId {FacId}. Reverting to QUEUED.", GetJobName(), emailToProcess.FacId);
                        
                        // Clear any pending changes for this facility
                        _dbContext.ChangeTracker.Clear();
                        
                        // Revert this specific record to QUEUED
                        emailToProcess.StatusCode = "QUEUED";
                        _dbContext.EmailToProcessQueue.Update(emailToProcess);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    facilitiesWithoutRecipients++;
                    _logger.LogWarning("{JobName}: No email recipients found for facility ID: {FacId}. Changing status to QUEUED for retry.", GetJobName(), emailToProcess.FacId);
                    emailToProcess.StatusCode = "QUEUED";
                    _dbContext.EmailToProcessQueue.Update(emailToProcess);
                    await _dbContext.SaveChangesAsync();
                }
            }
            
            if (facilitiesWithoutRecipients > 0 || facilitiesWithErrors > 0)
            {
                _logger.LogWarning("{JobName}: Processed {QueueCount} queue items, created {EmailCount} emails. {NoRecipientCount} facilities had no recipients, {ErrorCount} facilities had errors", 
                    GetJobName(), inQueue.Count, totalEmailsCreated, facilitiesWithoutRecipients, facilitiesWithErrors);
            }
            else
            {
                _logger.LogInformation("{JobName}: Processed {QueueCount} queue items, created {EmailCount} emails", GetJobName(), inQueue.Count, totalEmailsCreated);
            }

            return totalEmailsCreated;
        }
    }
}