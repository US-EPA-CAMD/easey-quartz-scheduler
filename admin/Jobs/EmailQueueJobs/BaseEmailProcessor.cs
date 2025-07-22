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

        protected async Task<Dictionary<decimal, HashSet<string>>> ProcessEmailRecipients()
        {
            string jobName = GetJobName();
            _logger.LogInformation("Executing {JobName} job. JobId: {JobId}", jobName, _jobId);

            JobLog jl = await CreateJobLogEntry(jobName);
            
            try
            {
                string emailTypeForDb = GetEmailTypeForDatabase();
                
                // Get queued emails for this type
                List<EmailToProcess> inQueue = _dbContext.EmailToProcessQueue.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.email_to_process
                    WHERE status_cd = 'QUEUED' AND email_type = {0}", emailTypeForDb).ToList();

                if (inQueue.Count == 0)
                {
                    _logger.LogInformation("No queued emails found for type: {EmailType}", emailTypeForDb);
                    await CompleteJobLog(jl);
                    return new Dictionary<decimal, HashSet<string>>();
                }

                // Mark records as WIP and collect plant IDs
                HashSet<long> plantIdSet = new HashSet<long>();
                foreach (EmailToProcess process in inQueue)
                {
                    process.StatusCode = "WIP";
                    _dbContext.EmailToProcessQueue.Update(process);
                    plantIdSet.Add(Convert.ToInt64(process.FacId));
                }
                _dbContext.SaveChanges();

                // Create plant ID list for API call
                long[] plantIdList = new long[plantIdSet.Count];
                plantIdSet.CopyTo(plantIdList);

                // Call recipient API
                string emailTypeForRecipientApi = GetEmailTypeForRecipientApi();
                RecipientResponse recipientResponse = await CallRecipientAPI(emailTypeForRecipientApi, plantIdList);

                // Process response and build facility-to-emails mapping
                Dictionary<decimal, HashSet<string>> facIdToEmails = BuildFacilityEmailMapping(recipientResponse);

                // Create EmailToSend records
                await CreateEmailToSendRecords(inQueue, facIdToEmails);

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
            _logger.LogError(e, "Error executing {JobName} job. JobId: {JobId}", GetJobName(), _jobId);
        }

        private async Task<RecipientResponse> CallRecipientAPI(string emailType, long[] plantIdList)
        {
            // Fire API Call
            ReminderNotificationPayload payload = new ReminderNotificationPayload()
            {
                plantIdList = plantIdList,
                emailType = emailType,
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

            _logger.LogInformation("Sending POST request to /support/email/emailRecipientList for emailType: {EmailType}", emailType);

            HttpResponseMessage response = await client.PostAsync(_configuration["EASEY_CAMD_SERVICES"] + "/support/email/emailRecipientList", httpContent);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            
            RecipientResponse recipientResponse = JsonConvert.DeserializeObject<RecipientResponse>(responseContent);

            // Check for API errors
            if (recipientResponse.hasError)
            {
                _logger.LogError("Recipient List API returned error: {ErrorMessage}", recipientResponse.errorMessage);
                throw new Exception($"Recipient List API returned error: {recipientResponse.errorMessage}");
            }

            // Log summary and first recipient if available
            int recipientCount = recipientResponse.recipients?.Length ?? 0;
            if (recipientCount > 0)
            {
                string firstRecipient = recipientResponse.recipients[0].emailAddressList;
                _logger.LogInformation("Successfully retrieved {RecipientCount} recipients for emailType: {EmailType}. First recipient: {FirstRecipient}", 
                    recipientCount, emailType, firstRecipient);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved {RecipientCount} recipients for emailType: {EmailType}", recipientCount, emailType);
            }
            return recipientResponse;
        }

        private Dictionary<decimal, HashSet<string>> BuildFacilityEmailMapping(RecipientResponse recipientResponse)
        {
            Dictionary<decimal, HashSet<string>> facIdToEmails = new Dictionary<decimal, HashSet<string>>();

            if (recipientResponse.recipients == null || recipientResponse.recipients.Length == 0)
            {
                _logger.LogWarning("No recipients found in API response");
                return facIdToEmails;
            }

            foreach (Recipient r in recipientResponse.recipients)
            {
                // Parse the single email address (may have display name format)
                string emailAddress = r.emailAddressList;
                
                if (string.IsNullOrEmpty(emailAddress))
                {
                    _logger.LogWarning("Empty email address found in recipient response");
                    continue;
                }

                foreach (long facId in r.plantIdList)
                {
                    decimal facIdDecimal = Convert.ToDecimal(facId);
                    
                    if (facIdToEmails.ContainsKey(facIdDecimal))
                    {
                        facIdToEmails[facIdDecimal].Add(emailAddress);
                    }
                    else
                    {
                        HashSet<string> emails = new HashSet<string> { emailAddress };
                        facIdToEmails.Add(facIdDecimal, emails);
                    }
                }
            }

            _logger.LogInformation("Built email mapping for {FacilityCount} facilities", facIdToEmails.Count);
            return facIdToEmails;
        }

        private async Task CreateEmailToSendRecords(List<EmailToProcess> inQueue, Dictionary<decimal, HashSet<string>> facIdToEmails)
        {
            foreach (EmailToProcess process in inQueue)
            {
                HashSet<string> allEmailsForFacility = new HashSet<string>();

                // Add recipients from API response
                if (facIdToEmails.ContainsKey(process.FacId))
                {
                    foreach (string email in facIdToEmails[process.FacId])
                    {
                        allEmailsForFacility.Add(email);
                    }
                }

                if (allEmailsForFacility.Count > 0)
                {
                    foreach (string emailTo in allEmailsForFacility)
                    {
                        EmailToSend es = new EmailToSend()
                        {
                            Context = process.Context,
                            StatusCode = "QUEUED",
                            TemplateId = process.EventCode,
                            ToEmail = emailTo,
                            FromEmail = _configuration["EASEY_QUARTZ_SCHEDULER_WINDOW_NOTIFICATION_FROM_EMAIL"]
                        };

                        _dbContext.EmailToSend.Add(es);
                    }
                }
                else
                {
                    _logger.LogWarning("No email recipients found for facility ID: {FacId}.", process.FacId);
                }

                // Mark as COMPLETE only after EMAIL_TO_SEND records are created
                process.StatusCode = "COMPLETE";
                _dbContext.EmailToProcessQueue.Update(process);
            }

            _dbContext.SaveChanges();
            _logger.LogInformation("Successfully processed {QueueCount} email queue items", inQueue.Count);
        }
    }
}