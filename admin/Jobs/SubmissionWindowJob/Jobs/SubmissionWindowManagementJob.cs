using Quartz;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace EaseyQuartz.Admin.Jobs
{
    /// <summary>
    /// Job responsible for managing submission windows and sending notifications
    /// to appropriate recipients based on window status.
    /// Built for .NET 8
    /// </summary>
    public class SubmissionWindowManagementJob : IJob
    {
        private readonly ILogger<SubmissionWindowManagementJob> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        // Environment variable keys
        private const string SMTP_HOST_KEY = "EASEY_QUARTZ_SCHEDULER_SMTP_HOST";
        private const string SMTP_PORT_KEY = "EASEY_QUARTZ_SCHEDULER_SMTP_PORT";
        private const string SENDER_EMAIL_KEY = "EASEY_QUARTZ_SCHEDULER_EMAIL";
        private const string AUTH_API_KEY = "EASEY_AUTH_API";

        public SubmissionWindowManagementJob(
            ILogger<SubmissionWindowManagementJob> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(configuration);

            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var requiredVars = new[] { SMTP_HOST_KEY, SMTP_PORT_KEY, SENDER_EMAIL_KEY, AUTH_API_KEY };
            foreach (var var in requiredVars)
            {
                if (string.IsNullOrEmpty(_configuration[var]))
                {
                    throw new InvalidOperationException($"Required environment variable {var} is not set");
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting Submission Window Management Job at: {time}", DateTimeOffset.Now);

                var dataMap = context.JobDetail.JobDataMap;
                var eventType = dataMap.GetString("EventType") ?? "SUBMISSIONREMINDER";
                var windowId = dataMap.GetString("WindowId"); // Optional, used for CLOSEWINDOW

                // Get recipients from the auth API
                var recipients = await GetRecipientsAsync(eventType);

                switch (eventType)
                {
                    case "SUBMISSIONREMINDER":
                        await HandleSubmissionReminder(recipients);
                        break;
                    case "WINDOWNOTIFICATION":
                        await HandleWindowNotification(recipients);
                        break;
                    case "CLOSEWINDOW":
                        ArgumentException.ThrowIfNullOrEmpty(windowId, nameof(windowId));
                        await HandleCloseWindow(recipients, windowId);
                        break;
                    default:
                        _logger.LogWarning("Unknown event type: {eventType}", eventType);
                        break;
                }

                _logger.LogInformation("Completed Submission Window Management Job at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Submission Window Management Job");
                throw;
            }
        }

        private async Task<RecipientList> GetRecipientsAsync(string eventType)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("AuthApi");
                client.BaseAddress = new Uri(_configuration[AUTH_API_KEY]);
                
                var response = await client.GetAsync($"/api/recipients/{eventType}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<RecipientList>(_jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recipients for event type: {eventType}", eventType);
                throw;
            }
        }

        private async Task HandleSubmissionReminder(RecipientList recipients)
        {
            // Filter recipients according to SUBMISSIONREMINDER rules
            var validRecipients = recipients.Agents
                .Where(a => a.IsActive)
                .Where(a => a.Type == "SMPQAEM" || 
                           (a.Type == "DR" && !recipients.Agents.Any(x => x.Type == "SMPQAEM" && x.IsActive)))
                .Where(a => a.Type != "SMP" && 
                           a.Type != "SMPQA" && 
                           a.Email != "ecmps_event_emails@erg.com")
                .ToList();

            await SendNotifications(validRecipients, "SUBMISSIONREMINDER");
        }

        private async Task HandleWindowNotification(RecipientList recipients)
        {
            // Filter recipients according to WINDOWNOTIFICATION rules
            var validRecipients = recipients.Agents
                .Where(a => a.IsActive)
                .Where(a => a.Type == "DR" || 
                           a.Type == "ADR" || 
                           a.Type == "SMPQAEM")
                .Where(a => a.Type != "SMP" && 
                           a.Type != "SMPQA" && 
                           a.Email != "ecmps_event_emails@erg.com")
                .ToList();

            await SendNotifications(validRecipients, "WINDOWNOTIFICATION");
        }

        private async Task HandleCloseWindow(RecipientList recipients, string windowId)
        {
            try
            {
                await CloseSubmissionWindow(windowId);

                var validRecipients = recipients.Agents
                    .Where(a => a.IsActive)
                    .Where(a => a.Type == "DR" || 
                               a.Type == "ADR" || 
                               a.Type == "SMPQAEM")
                    .Where(a => a.Type != "SMP" && 
                               a.Type != "SMPQA" && 
                               a.Email != "ecmps_event_emails@erg.com")
                    .ToList();

                await SendNotifications(validRecipients, "CLOSEWINDOW", windowId);

                _logger.LogInformation("Successfully closed submission window {windowId}", windowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing submission window {windowId}", windowId);
                throw;
            }
        }

        private async Task CloseSubmissionWindow(string windowId)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("AuthApi");
                var response = await client.PutAsync($"/api/submission-windows/{windowId}/close", null);
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("Submission window {windowId} closed successfully", windowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close submission window {windowId}", windowId);
                throw;
            }
        }

        private async Task SendNotifications(IEnumerable<Agent> recipients, string eventType, string windowId = null)
        {
            var smtpHost = _configuration[SMTP_HOST_KEY];
            var smtpPort = int.Parse(_configuration[SMTP_PORT_KEY]);
            var senderEmail = _configuration[SENDER_EMAIL_KEY];

            using var smtpClient = new SmtpClient(smtpHost, smtpPort);

            foreach (var recipient in recipients)
            {
                try
                {
                    var subject = GetEmailSubject(eventType, windowId);
                    var body = await GetEmailBody(eventType, recipient, windowId);

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(recipient.Email);

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation(
                        "Successfully sent {eventType} notification to {recipientType} at {email}", 
                        eventType, 
                        recipient.Type, 
                        recipient.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error sending {eventType} notification to {recipientType} at {email}", 
                        eventType, 
                        recipient.Type, 
                        recipient.Email);
                }
            }
        }

        private static string GetEmailSubject(string eventType, string windowId = null) =>
            eventType switch
            {
                "SUBMISSIONREMINDER" => "ECMPS Submission Window Reminder",
                "WINDOWNOTIFICATION" => "ECMPS Submission Window Status Update",
                "CLOSEWINDOW" => $"ECMPS Submission Window {windowId} Closed",
                _ => "ECMPS Notification"
            };

        private static Task<string> GetEmailBody(string eventType, Agent recipient, string windowId = null)
        {
            var baseTemplate = $$"""
                <html>
                <body>
                    <h2>ECMPS {{eventType}}</h2>
                    <p>Dear {{recipient.Type}},</p>
                """;

            var content = eventType switch
            {
                "CLOSEWINDOW" => $$"""
                    <p>The submission window (ID: {{windowId}}) has been closed.</p>
                    <p>No further submissions will be accepted for this window.</p>
                    <p>Please review any pending submissions in your ECMPS dashboard.</p>
                    """,
                
                "SUBMISSIONREMINDER" => """
                    <p>This is a reminder about the current submission window.</p>
                    <p>Please ensure all required submissions are completed before the window closes.</p>
                    """,
                
                "WINDOWNOTIFICATION" => """
                    <p>This is a notification regarding the submission window status.</p>
                    <p>Please check your ECMPS dashboard for more information.</p>
                    """,
                
                _ => """
                    <p>This is a notification regarding the submission window.</p>
                    <p>Please check your ECMPS dashboard for more information.</p>
                    """
            };

            var footer = """
                    <br/>
                    <p>Best regards,</p>
                    <p>ECMPS System</p>
                </body>
                </html>
                """;

            return Task.FromResult($"{baseTemplate}{content}{footer}");
        }
    }

    public sealed record RecipientList(List<Agent> Agents);

    public sealed record Agent(string Type, string Email, bool IsActive);
}
