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
        List<EmailGroup> GroupEmailRecords(List<EmailToProcess> emailRecords, RecipientResponse recipientResponse, ILogger logger = null);
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
        public List<EmailGroup> GroupEmailRecords(List<EmailToProcess> emailRecords, RecipientResponse recipientResponse, ILogger logger = null)
        {
            var groupedEmails = new List<EmailGroup>();
            
            // Build mapping: recipient email -> list of facility IDs they manage
            var recipientToFacilities = BuildRecipientToFacilitiesMapping(recipientResponse, logger);
            
            // Create one email group per recipient
            foreach (var (recipient, facilities) in recipientToFacilities)
            {
                // Find all email records for facilities this recipient manages
                var recipientEmailRecords = emailRecords
                    .Where(er => facilities.Contains(er.FacId))
                    .ToList();
                    
                if (recipientEmailRecords.Any())
                {
                    groupedEmails.Add(new EmailGroup
                    {
                        GroupKey = recipient,
                        Recipients = [recipient],
                        EmailRecords = recipientEmailRecords,
                        CombinedContext = CombineContexts(recipientEmailRecords, logger), // JSON array of all contexts
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
        private Dictionary<string, List<decimal>> BuildRecipientToFacilitiesMapping(RecipientResponse recipientResponse, ILogger logger)
        {
            var mapping = new Dictionary<string, List<decimal>>();
            int skippedRecipients = 0;
            
            foreach (var recipient in recipientResponse.recipients ?? [])
            {
                var email = recipient.emailAddressList;
                if (!string.IsNullOrEmpty(email))
                {
                    if (!mapping.ContainsKey(email))
                    {
                        mapping[email] = new List<decimal>();
                    }
                    mapping[email].AddRange( recipient.plantIdList.Select(id => Convert.ToDecimal(id)) );
                }
                else
                {
                    skippedRecipients++;
                }
            }
            
            if (skippedRecipients > 0)
            {
                logger?.LogWarning("Skipped {SkippedCount} recipients due to missing email addresses", skippedRecipients);
            }
            
            return mapping;
        }
        
        /// <summary>
        /// Combines multiple facility contexts into a template-ready object for submission reminders
        /// 
        /// Input: List of EmailToProcess records, each with Context field containing JSON string
        /// Example Context values for recipient managing Severstal Sparrows Point LLC (orisCode: 10485):
        /// - Record 1.Context: {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR1", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"}
        /// - Record 2.Context: {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR4", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"}
        /// - Record 3.Context: {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR3", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"}
        /// 
        /// Output: JSON object with emSubAccessList array for template loop processing:
        /// {
        ///   "emSubAccessList": [
        ///     {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR1", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"},
        ///     {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR4", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"},
        ///     {"orisCode": 10485, "plantName": "Severstal Sparrows Point LLC", "plantState": "MD", "locationList": "BLR3", "windowOpenDate": "2010-10-01", "periodAbbreviation": "2010 Q3"}
        ///   ]
        /// }
        /// </summary>
        private string CombineContexts(List<EmailToProcess> emailRecords, ILogger logger = null)
        {
            var facilities = emailRecords
                .Where(er => !string.IsNullOrEmpty(er.Context))
                .Select(er => {
                    try 
                    {
                        return JsonConvert.DeserializeObject(er.Context);
                    }
                    catch (JsonException ex)
                    {
                        logger?.LogWarning("SubmissionReminderGrouping: Skipping record with invalid JSON context. ToProcessId: {ToProcessId}, Error: {Error}", 
                            er.ProcessId, ex.Message);
                        return null;
                    }
                })
                .Where(facility => facility != null)
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
            RecipientResponse recipientResponse,
            ILogger logger = null)
        {
            var groupedEmails = new List<EmailGroup>();
            
            // Build mapping: facility ID -> list of recipient emails
            var facilityToRecipients = BuildFacilityToRecipientsMapping(recipientResponse, logger);
            
            // Group by facility ID to combine multiple monitoring locations
            var facilityGroups = emailRecords.GroupBy(er => er.FacId);
            
            // Create one email group per facility
            foreach (var facilityGroup in facilityGroups)
            {
                var facId = facilityGroup.Key;
                var facilityEmailRecords = facilityGroup.ToList();
                
                if (facilityToRecipients.ContainsKey(facId))
                {
                    groupedEmails.Add(new EmailGroup
                    {
                        GroupKey = facId.ToString(),
                        Recipients = facilityToRecipients[facId], // All recipients for this facility
                        EmailRecords = facilityEmailRecords,      // All monitoring locations for this facility
                        CombinedContext = CombineContexts(facilityEmailRecords, logger), // JSON array of all location contexts
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
        private Dictionary<decimal, List<string>> BuildFacilityToRecipientsMapping(
            RecipientResponse recipientResponse, ILogger logger)
        {
            var mapping = new Dictionary<decimal, List<string>>();
            int skippedRecipients = 0;
            
            foreach (var recipient in recipientResponse.recipients ?? [])
            {
                var email = recipient.emailAddressList;
                if (!string.IsNullOrEmpty(email))
                {
                    foreach (var plantId in recipient.plantIdList)
                    {
                        var facId = Convert.ToDecimal(plantId);
                        if (!mapping.ContainsKey(facId))
                        {
                            mapping[facId] = new List<string>();
                        }

                        // Add recipient to facility's list if not already present
                        if (!mapping[facId].Contains(email)) mapping[facId].Add(email);
                    }
                }
                else
                {
                    skippedRecipients++;
                    logger?.LogWarning("WindowNotificationGrouping: Skipping recipient with null/empty emailAddressList. PlantIds: [{PlantIds}]", 
                        string.Join(", ", recipient.plantIdList ?? []));
                }
            }
            
            if (skippedRecipients > 0)
            {
                logger?.LogWarning("WindowNotificationGrouping: Skipped {SkippedCount} recipients due to missing email addresses", skippedRecipients);
            }
            
            return mapping;
        }
        
        /// <summary>
        /// Combines multiple monitoring location contexts into a template-ready object for window notifications
        /// 
        /// Input: List of EmailToProcess records for different locations within the same facility
        /// Example Context values for Vandolah Power Project (orisCode: 55415):
        /// - Record 1.Context: {"orisCode": 55415, "plantName": "Vandolah Power Project", "plantState": "FL", "locationList": "GT401", "windowOpenDate": "2023-11-06", "periodAbbreviation": "2023 Q2"}
        /// - Record 2.Context: {"orisCode": 55415, "plantName": "Vandolah Power Project", "plantState": "FL", "locationList": "GT401", "windowOpenDate": "2023-11-06", "periodAbbreviation": "2023 Q3"}
        /// 
        /// Output: JSON object with emSubAccessList array for template loop processing:
        /// {
        ///   "emSubAccessList": [
        ///     {"orisCode": 55415, "plantName": "Vandolah Power Project", "plantState": "FL", "locationList": "GT401", "windowOpenDate": "2023-11-06", "periodAbbreviation": "2023 Q2"},
        ///     {"orisCode": 55415, "plantName": "Vandolah Power Project", "plantState": "FL", "locationList": "GT401", "windowOpenDate": "2023-11-06", "periodAbbreviation": "2023 Q3"}
        ///   ]
        /// }
        /// </summary>
        private string CombineContexts(List<EmailToProcess> emailRecords, ILogger logger = null)
        {
            var facilities = emailRecords
                .Where(er => !string.IsNullOrEmpty(er.Context))
                .Select(er => {
                    try 
                    {
                        return JsonConvert.DeserializeObject(er.Context);
                    }
                    catch (JsonException ex)
                    {
                        logger?.LogWarning("WindowNotificationGrouping: Skipping record with invalid JSON context. ToProcessId: {ToProcessId}, Error: {Error}", 
                            er.ProcessId, ex.Message);
                        return null;
                    }
                })
                .Where(facility => facility != null)
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