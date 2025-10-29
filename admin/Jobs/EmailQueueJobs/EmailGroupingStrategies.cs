using System;
using System.Collections.Generic;
using System.Linq;
using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Epa.Camd.Quartz.Scheduler.Jobs.EmailQueueJobs
{
    /// <summary>
    /// Strategy interface for grouping email records based on a key
    /// </summary>
    public interface IEmailGroupingStrategy
    {
        List<EmailGroup> GroupEmailRecords(List<EmailToProcess> emailRecords, List<IndividualRecipient> individualRecipients, ILogger logger = null);
    }
    
    /// <summary>
    /// Represents a group of emails to be sent together
    /// </summary>
    public class EmailGroup
    {
        public string GroupKey { get; set; }
        public List<string> Recipients { get; set; } = new List<string>();
        public List<EmailToProcess> EmailRecords { get; set; } = new List<EmailToProcess>();
        public string CombinedContext { get; set; }
        public long? TemplateId { get; set; }
    }
    
    /// <summary>
    /// Represents the facility context data from email_to_process.context JSON
    /// </summary>
    public class FacilityContext
    {
        [JsonProperty("plantName")]
        public string PlantName { get; set; }

        [JsonProperty("locationList")]
        public string LocationList { get; set; }

        [JsonProperty("periodAbbreviation")]
        public string PeriodAbbreviation { get; set; }

        [JsonProperty("plantState")]
        public string PlantState { get; set; }

        [JsonProperty("orisCode")]
        public int OrisCode { get; set; }

        [JsonProperty("windowOpenDate")]
        public string WindowOpenDate { get; set; }
    }
    
    /// <summary>
    /// Shared utility methods for email grouping strategies
    /// </summary>
    public static class EmailGroupingUtils
    {
        /// <summary>
        /// Combines multiple facility contexts into a template-ready JSON object
        /// 
        /// Input: List of EmailToProcess records, each with Context field containing JSON string
        /// 
        /// Output: JSON object with emSubAccessList array for template loop processing:
        /// {
        ///   "emSubAccessList": [
        ///     {"orisCode": 10485, "plantName": "Plant A", "plantState": "MD", "locationList": "BLR1", ...},
        ///     {"orisCode": 10485, "plantName": "Plant A", "plantState": "MD", "locationList": "BLR4", ...}
        ///   ]
        /// }
        /// </summary>
        /// <param name="emailRecords">List of email records to combine</param>
        /// <param name="operationName">Name of the operation for logging (e.g., "SubmissionReminder", "WindowNotification")</param>
        /// <param name="logger">Optional logger for warnings</param>
        /// <returns>JSON string with emSubAccessList array, or null if no valid facilities</returns>
        public static string CombineContexts(List<EmailToProcess> emailRecords, string operationName = "EmailGrouping", ILogger logger = null)
        {
            var facilities = emailRecords
                .Where(er => !string.IsNullOrEmpty(er.Context))
                .Select(er => {
                    try 
                    {
                        return JsonConvert.DeserializeObject<FacilityContext>(er.Context);
                    }
                    catch (JsonException ex)
                    {
                        logger?.LogWarning("{OperationName}: Skipping record with invalid JSON context. ToProcessId: {ToProcessId}, Error: {Error}", 
                            operationName, er.ProcessId, ex.Message);
                        return null;
                    }
                })
                .Where(facility => facility != null)
                .OrderBy(facility => facility.PlantName ?? "")
                .ThenBy(facility => facility.LocationList ?? "")
                .ThenBy(facility => facility.PeriodAbbreviation ?? "")
                .ToList();

            if (!facilities.Any())
            {
                return null;
            }

            var templateContext = new { emSubAccessList = facilities };
            return JsonConvert.SerializeObject(templateContext);
        }
    }
    
    /// <summary>
    /// Groups emails by recipient - one email per person containing all their facilities
    /// 
    /// Example Input:
    /// - EmailToProcess records: [FacId=628, FacId=629, FacId=630]
    /// - RecipientResponse: 
    ///   * john@epa.gov -> [628, 629]
    ///   * david@epa.gov -> [629, 630]
    /// 
    /// Example Output:
    /// - Group 1: john@epa.gov receives 1 email about facilities [628, 629]
    /// - Group 2: david@epa.gov receives 1 email about facilities [629, 630]
    /// </summary>
    public class SubmissionReminderGroupingStrategy : IEmailGroupingStrategy
    {
        public List<EmailGroup> GroupEmailRecords(List<EmailToProcess> emailRecords, List<IndividualRecipient> individualRecipients, ILogger logger = null)
        {
            var groupedEmails = new List<EmailGroup>();
            
            // Build mapping: recipient email -> list of facility IDs they manage
            var recipientToFacilities = BuildRecipientToFacilitiesMapping(individualRecipients, logger);
            
            // Create one email group per recipient
            foreach (var (recipient, facilities) in recipientToFacilities)
            {
                // Find all email records for facilities this recipient manages
                var recipientEmailRecords = emailRecords
                    .Where(er => facilities.Contains(Convert.ToInt64(er.FacId))) // FIXED: Convert decimal to long
                    .ToList();
                    
                if (recipientEmailRecords.Any())
                {
                    var combinedContext = EmailGroupingUtils.CombineContexts(recipientEmailRecords, "SubmissionReminderGrouping", logger);
                    
                    groupedEmails.Add(new EmailGroup
                    {
                        GroupKey = recipient,
                        Recipients = new List<string> { recipient },
                        EmailRecords = recipientEmailRecords,
                        CombinedContext = combinedContext,
                        TemplateId = recipientEmailRecords.First().EventCode
                    });
                }
            }
            
            return groupedEmails;
        }
        
        /// <summary>
        /// Creates mapping of recipient email to their managed facilities
        /// Example: {"john@epa.gov": [628, 629], "david@epa.gov": [629, 630]}
        /// </summary>
        private Dictionary<string, List<long>> BuildRecipientToFacilitiesMapping(List<IndividualRecipient> individualRecipients, ILogger logger)
        {
            var mapping = new Dictionary<string, List<long>>();
            int skippedRecipients = 0;
            
            foreach (var recipient in individualRecipients)
            {
                var email = recipient.Email;
                if (!string.IsNullOrEmpty(email))
                {
                    if (!mapping.ContainsKey(email))
                    {
                        mapping[email] = new List<long>();
                    }
                    
                    // Add all facility IDs for this recipient
                    mapping[email].AddRange(recipient.FacilityIds);
                }
                else
                {
                    skippedRecipients++;
                }
            }
            
            // Remove duplicates from each recipient's facility list
            foreach (var email in mapping.Keys.ToList())
            {
                mapping[email] = mapping[email].Distinct().ToList();
            }
            
            if (skippedRecipients > 0)
            {
                logger?.LogWarning("SubmissionReminderGrouping: Skipped {SkippedCount} recipients due to missing email addresses", skippedRecipients);
            }
            
            return mapping;
        }
    }
    
    /// <summary>
    /// Groups emails by facility - one email per facility sent to all its recipients
    /// 
    /// Example Input:
    /// - EmailToProcess records: [FacId=628 (2 locations), FacId=629 (1 location)]
    /// - RecipientResponse:
    ///   * john@epa.gov -> [628, 629]
    ///   * david@epa.gov -> [628]
    ///   * bob@epa.gov -> [629]
    /// 
    /// Example Output:
    /// - Group 1: Facility 628 email sent to [john@epa.gov, david@epa.gov] with combined contexts
    /// - Group 2: Facility 629 email sent to [john@epa.gov, bob@epa.gov] with single context
    /// </summary>
    public class WindowNotificationGroupingStrategy : IEmailGroupingStrategy
    {
        public List<EmailGroup> GroupEmailRecords(
            List<EmailToProcess> emailRecords, 
            List<IndividualRecipient> individualRecipients,
            ILogger logger = null)
        {
            var groupedEmails = new List<EmailGroup>();
            
            // Build mapping: facility ID -> list of recipient emails
            var facilityToRecipients = BuildFacilityToRecipientsMapping(individualRecipients, logger);
            
            // Group by facility ID to combine multiple monitoring locations
            var facilityGroups = emailRecords.GroupBy(er => er.FacId);
            
            // Create one email group per facility
            foreach (var facilityGroup in facilityGroups)
            {
                var facId = facilityGroup.Key;
                var facilityEmailRecords = facilityGroup.ToList();
                var facilityIdLong = Convert.ToInt64(facId); // Convert decimal to long
                
                if (facilityToRecipients.ContainsKey(facilityIdLong))
                {
                    var recipients = facilityToRecipients[facilityIdLong];
                    var combinedContext = EmailGroupingUtils.CombineContexts(facilityEmailRecords, "WindowNotificationGrouping", logger);
                    
                    groupedEmails.Add(new EmailGroup
                    {
                        GroupKey = facId.ToString(),
                        Recipients = recipients, // All recipients for this facility
                        EmailRecords = facilityEmailRecords, // All monitoring locations for this facility
                        CombinedContext = combinedContext,
                        TemplateId = facilityEmailRecords.First().EventCode
                    });
                }
            }
            
            return groupedEmails;
        }
        
        /// <summary>
        /// Creates mapping of facility ID to all its recipient emails
        /// Example: {628: ["john@epa.gov", "david@epa.gov"], 629: ["john@epa.gov", "bob@epa.gov"]}
        /// </summary>
        private Dictionary<long, List<string>> BuildFacilityToRecipientsMapping(
            List<IndividualRecipient> individualRecipients, ILogger logger)
        {
            var mapping = new Dictionary<long, List<string>>();
            int skippedRecipients = 0;
            
            foreach (var recipient in individualRecipients)
            {
                var email = recipient.Email;
                if (!string.IsNullOrEmpty(email))
                {
                    foreach (var facilityId in recipient.FacilityIds)
                    {
                        if (!mapping.ContainsKey(facilityId))
                        {
                            mapping[facilityId] = new List<string>();
                        }

                        // Add recipient to facility's list if not already present
                        if (!mapping[facilityId].Contains(email)) 
                        {
                            mapping[facilityId].Add(email);
                        }
                    }
                }
                else
                {
                    skippedRecipients++;
                    logger?.LogWarning("WindowNotificationGrouping: Skipping recipient with null/empty email. FacilityIds: {FacilityIds}", 
                        string.Join(", ", recipient.FacilityIds));
                }
            }
            
            if (skippedRecipients > 0)
            {
                logger?.LogWarning("WindowNotificationGrouping: Skipped {SkippedCount} recipients due to missing email addresses", skippedRecipients);
            }
            
            return mapping;
        }
    }
    
    /// <summary>
    /// Factory for creating appropriate grouping strategy based on email type
    /// </summary>
    public static class EmailGroupingStrategyFactory
    {
        public static IEmailGroupingStrategy CreateStrategy(string emailType)
        {
            return emailType switch
            {
                // Submission reminders: Group by recipient (one email per person)
                BaseEmailProcessor.SubmissionReminderEmailTypeForDb =>  new SubmissionReminderGroupingStrategy(),
                    
                // Window notifications: Group by facility (one email per facility)
                BaseEmailProcessor.WindowNotificationEmailTypeForDb =>  new WindowNotificationGroupingStrategy(),
                    
                _ => throw new ArgumentException($"Unknown email type: {emailType}")
            };
        }
    }
}