using System;
using System.Threading.Tasks;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class SubmissionJobQueue : IJob, IJobMetadata<SubmissionJobQueue>
  {
    public static string JobName => "Submission Job Queue";
    public static string JobDescription => "Operates on an interval to determine if sets in SubmissionSet table can be submitted.";
    public static string JobGroup => Constants.QuartzGroups.MAINTAINANCE;
    public static string TriggerName => "Submission Job Queue Trigger";
    public static string TriggerDescription => "Operate every minute to determine if there are files in submission queue which can be triggered";

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<SubmissionJobQueue> _logger;
    private IConfiguration Configuration { get; }

    public SubmissionJobQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<SubmissionJobQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        _logger.LogInformation("Starting submission job execution. Checking QUEUED status submissions ");

        int inQueueCount = _dbContext.SubmissionSet.Count(s => s.StatusCode == "QUEUED");

        _logger.LogInformation("Found {InQueueCount} submission sets in QUEUED status", inQueueCount);

        if (inQueueCount == 0) return;

        int wipOrClaimedCount = _dbContext.SubmissionSet.Count(s => s.StatusCode == "WIP" || s.StatusCode == "CLAIMED");

        _logger.LogInformation("Found {InQueueCount} items in queue and {InWIPCount} items in WIP or CLAIMED", inQueueCount, wipOrClaimedCount);

        var maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"]);

        // Exit if max number of allowed jobs already in progress
        if (wipOrClaimedCount >= maxAllowed) {
            _logger.LogInformation("Maximum number of submission jobs ({MaxJobs}) already in progress. Skipping processing...", maxAllowed);
            return;
        }

        int jobs_to_schedule = maxAllowed - wipOrClaimedCount;
        _logger.LogInformation("Scheduling {JobsToSchedule} jobs", jobs_to_schedule);

        // Set to CLAIMED
        List<SubmissionSet> claimed = _dbContext.SubmissionSet
          .FromSqlRaw(@"
              UPDATE camdecmpsaux.submission_set
              SET status_cd = 'CLAIMED'
              WHERE submission_set_id IN (
                  SELECT submission_set_id
                  FROM camdecmpsaux.submission_set
                  WHERE status_cd = 'QUEUED'
                  ORDER BY queued_time
                  LIMIT {0}
                  FOR UPDATE SKIP LOCKED
              )
              RETURNING *;",
              jobs_to_schedule)
          .ToList();

        _logger.LogInformation("Claimed {ClaimedCount} submission sets for processing", claimed?.Count ?? 0);

        string clientToken = await Utils.generateClientToken();

        foreach (var setRecord in claimed)
        {
          _logger.LogInformation("Submitting to camd-services SubmissionSetId {SubmissionSetId}", setRecord.SetId);

          try
          {
            await SubmitSet(setRecord.SetId, clientToken);
          }
          catch (Exception ex)
          {
            HandleSubmissionError(setRecord, ex);
          }

          Thread.Sleep(Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_SUBMISSION_JOB_QUEUE_DELAY"] ?? "1") * 1000);
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e, "An error occurred while executing a submission job at {Time}", DateTimeOffset.Now);
        return;
      }
    }

    /// <summary>
    /// Build the email context for submission error notifications.
    /// </summary>
    /// <param name="setRecord">The submission set record that encountered an error.</param>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="errorTime">The time the error occurred.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="fromEmail">The sender email address.</param>
    private string BuildErrorEmailContext(SubmissionSet setRecord, Exception ex, DateTime errorTime, string toEmail, string fromEmail)
    {
      var context = new
      {
        argumentValues = $"Submission set: {setRecord.SetId}",
        configuration = setRecord.Config,
        errorDate = errorTime.ToString("g"), // mm/dd/yyyy hh:mm AM/PM
        errorDetails = ex.ToString(),
        errorId = Guid.NewGuid().ToString(),
        errorMessage = ex.Message,
        orisCode = setRecord.OrisCode,
        stages = new List<object> {},
        submissionDateDisplay = setRecord.QueuedTime.ToString("MMMM dd, yyyy 'at' h:mm tt"),
        submissionId = setRecord.SetId,
        submitter = setRecord.UserId,
        yearQtr = "N/A", // The error occurred before processing any individual submissions, so this is not applicable
      };
      return JsonConvert.SerializeObject(context);
    }

    /// <summary>
    /// Handle errors that occur during submission processing. Updates the status of the submission set and associated submissions to "ERROR" and queues an error email.
    /// </summary>
    /// <param name="setRecord">The submission set record that encountered an error.</param>
    /// <param name="ex">The exception that was thrown.</param>
    private void HandleSubmissionError(SubmissionSet setRecord, Exception ex)
    {
      try
      {
        var errorTime = Utils.getCurrentEasternTime();

        // Update the status of the submission set to ERROR.
        setRecord.StatusCode = "ERROR";
        setRecord.Note = ex.Message;
        setRecord.NoteTime = errorTime;
        _dbContext.SubmissionSet.Update(setRecord);
        _dbContext.SaveChanges();

        var submissionsInSet = _dbContext.Submissions
          .Where(s => s.SetId == setRecord.SetId)
          .ToList();

        // Update the status of the associated submissions to ERROR.
        foreach (var submission in submissionsInSet)
        {
          submission.StatusCode = "ERROR";
          submission.Note = ex.Message;
          submission.NoteTime = errorTime;
          _dbContext.Submissions.Update(submission);
        }

        _dbContext.SaveChanges();

        QueueSubmissionErrorEmail(setRecord, ex, errorTime);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Failed to handle submission error for SubmissionSetId {SubmissionSetId}", setRecord.SetId);
      }
    }

    /// <summary>
    /// Queue an email notification for submission errors.
    /// </summary>
    /// <param name="setRecord">The submission set record that encountered an error.</param>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="errorTime">The time the error occurred.</param>
    private void QueueSubmissionErrorEmail(SubmissionSet setRecord, Exception ex, DateTime errorTime)
    {
      try
      {
          ClientConfig clientConfig = _dbContext.ClientConfigurations
            .Where(c => c.ClientName == "ecmps-ui")
            .FirstOrDefault();

          if (clientConfig == null || string.IsNullOrEmpty(clientConfig.SupportEmail))
          {
            _logger.LogWarning("Client configuration for 'ecmps-ui' not found or support email is missing. Cannot send submission error email for SubmissionSetId {SubmissionSetId}", setRecord.SetId);
            return;
          }

          var toEmail = clientConfig.SupportEmail;
          var fromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_SUBMISSION_ERROR_FROM_EMAIL"];

          EmailToSend emailToSend = new EmailToSend()
          {
            Context = BuildErrorEmailContext(setRecord, ex, errorTime, toEmail, fromEmail),
            StatusCode = "QUEUED",
            TemplateId = Constants.EmailTemplateIds.SUBMISSION_FAILURE_SUPPORT,
            ToEmail = toEmail,
            FromEmail = fromEmail,
          };
          _dbContext.EmailToSend.Add(emailToSend);
          _dbContext.SaveChanges();

          _logger.LogInformation("Queued submission error email for SubmissionSetId {SubmissionSetId}", setRecord.SetId);
      }
      catch (Exception e)
      {
          _logger.LogError(e, "Failed to send submission error email for SubmissionSetId {SubmissionSetId}", setRecord.SetId);
      }
    }

    /// <summary>
    /// Initiate submission processing by calling the camd-services API with retry logic and exponential backoff.
    /// </summary>
    /// <param name="setId">The submission set ID to process.</param>
    /// <param name="clientToken">The client token for authentication.</param>
    private async Task SubmitSet(string setId, string clientToken)
    {
        ToProcessSubmissionPayload payload = new ToProcessSubmissionPayload
        {
            submissionSetId = setId
        };

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

        const int maxRetries = 3;
        var rng = new Random();

        for (int retryCount = 0; retryCount < maxRetries; retryCount++)
        {
            try
            {
                using var httpContent = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await client.PostAsync(
                    Configuration["EASEY_CAMD_SERVICES"] + "/submission/process", 
                    httpContent);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "Submitted job for SubmissionSetId {SubmissionSetId} with response {ResponseStatusCode}",
                    setId, response.StatusCode);

                return; // success, exit method
            }
            catch (Exception e) when (retryCount < maxRetries - 1)
            {
                // Calculate exponential backoff with jitter (e.g., 0.5x to 1.5x of the base delay)
                int delayMs = (int)(Math.Pow(2, retryCount) * 1000);
                int jitter = rng.Next((int)(delayMs * 0.5), (int)(delayMs * 1.5));

                _logger.LogWarning(
                    e,
                    "Error submitting SubmissionSetId {SubmissionSetId}. Retrying in {DelayMs}ms... Attempt {RetryCount}/{MaxRetries}",
                    setId, jitter, retryCount + 1, maxRetries);

                await Task.Delay(jitter);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Failed to submit SubmissionSetId {SubmissionSetId} after {RetryCount} attempts",
                    setId, maxRetries);
                throw;
            }
        }
    }

  }
}
