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
  public class BulkImportJobQueue : IJob, IJobMetadata<BulkImportJobQueue>
  {
    public static string JobName => "Bulk Import Job Queue";
    public static string JobDescription => "Operates on an interval to hand queued bulk import sets to camd-services for processing.";
    public static string JobGroup => Constants.QuartzGroups.MAINTAINANCE;
    public static string TriggerName => "Bulk Import Job Queue Trigger";
    public static string TriggerDescription => "Operate on an interval to determine if there are bulk import sets which can be processed.";

    private readonly NpgSqlContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BulkImportJobQueue> _logger;

    public BulkImportJobQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<BulkImportJobQueue> logger)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        _logger.LogInformation("Starting bulk import job execution. Checking QUEUED import sets");

        int inQueueCount = _dbContext.ImportSet.Count(s => s.StatusCode == "QUEUED");
        _logger.LogInformation("Found {InQueueCount} import sets in QUEUED status", inQueueCount);

        if (inQueueCount == 0) return;

        int wipOrClaimedCount = _dbContext.ImportSet.Count(s => s.StatusCode == "CLAIMED" || s.StatusCode == "WIP");
        _logger.LogInformation("Found {InQueueCount} import sets in queue and {InWIPCount} in CLAIMED or WIP", inQueueCount, wipOrClaimedCount);

        var maxAllowed = Int32.Parse(_configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_IMPORT_JOBS"] ?? "1");

        if (wipOrClaimedCount >= maxAllowed)
        {
          _logger.LogInformation("Maximum number of import jobs ({MaxJobs}) already in progress. Skipping processing...", maxAllowed);
          return;
        }

        int jobs_to_schedule = maxAllowed - wipOrClaimedCount;
        _logger.LogInformation("Scheduling {JobsToSchedule} import jobs", jobs_to_schedule);

        // Claim by moving QUEUED -> CLAIMED (set claimed_time); the camd-services
        // process endpoint then transitions each set to WIP.
        List<ImportSet> claimed = _dbContext.ImportSet
          .FromSqlRaw(@"
              UPDATE camdecmpsaux.import_set
              SET claimed_time = {0}
              WHERE import_set_id IN (
                  SELECT import_set_id
                  FROM camdecmpsaux.import_set
                  WHERE status_cd = 'QUEUED'
                  ORDER BY queued_time
                  LIMIT {1}
                  FOR UPDATE SKIP LOCKED
              )
              RETURNING *;",
              Utils.getCurrentEasternTime(), jobs_to_schedule)
          .ToList();

        _logger.LogInformation("Claimed {ClaimedCount} import sets for processing", claimed?.Count ?? 0);

        string clientToken = await Utils.generateClientToken();

        foreach (var setRecord in claimed)
        {
          _logger.LogInformation("Submitting to camd-services ImportSetId {ImportSetId}", setRecord.ImportSetId);

          try
          {
            await ProcessSet(setRecord.ImportSetId, clientToken);
          }
          catch (Exception ex)
          {
            HandleDispatchError(setRecord, ex);
          }

          Thread.Sleep(Int32.Parse(_configuration["EASEY_QUARTZ_SCHEDULER_BULK_IMPORT_JOB_QUEUE_DELAY"] ?? "1") * 1000);
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e, "An error occurred while executing a bulk import job at {Time}", DateTimeOffset.Now);
        return;
      }
    }

    /// <summary>
    /// Marks the set ERROR (note / note_time) after retries are exhausted.
    /// </summary>
    private void HandleDispatchError(ImportSet setRecord, Exception ex)
    {
      try
      {
        _logger.LogError(ex, "Failed to dispatch ImportSetId {ImportSetId}", setRecord.ImportSetId);
        setRecord.Note = ex.Message;
        setRecord.NoteTime = Utils.getCurrentEasternTime();
        _dbContext.ImportSet.Update(setRecord);
        _dbContext.SaveChanges();
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Failed to mark ImportSetId {ImportSetId} as errored after a dispatch error", setRecord.ImportSetId);
      }
    }

    /// <summary>
    /// Calls the camd-services bulk import process endpoint with retry and exponential backoff.
    /// </summary>
    private async Task ProcessSet(string importSetId, string clientToken)
    {
      ToProcessImportPayload payload = new ToProcessImportPayload
      {
        importSetId = importSetId
      };

      using HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("x-api-key", _configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
      client.DefaultRequestHeaders.Add("x-client-id", _configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);
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
              _configuration["EASEY_CAMD_SERVICES"] + "/bulk-import/process",
              httpContent);

          response.EnsureSuccessStatusCode();

          _logger.LogInformation(
              "Dispatched import job for ImportSetId {ImportSetId} with response {ResponseStatusCode}",
              importSetId, response.StatusCode);

          return;
        }
        catch (Exception e) when (retryCount < maxRetries - 1)
        {
          int delayMs = (int)(Math.Pow(2, retryCount) * 1000);
          int jitter = rng.Next((int)(delayMs * 0.5), (int)(delayMs * 1.5));

          _logger.LogWarning(
              e,
              "Error dispatching ImportSetId {ImportSetId}. Retrying in {DelayMs}ms... Attempt {RetryCount}/{MaxRetries}",
              importSetId, jitter, retryCount + 1, maxRetries);

          await Task.Delay(jitter);
        }
        catch (Exception e)
        {
          _logger.LogError(
              e,
              "Failed to dispatch ImportSetId {ImportSetId} after {RetryCount} attempts",
              importSetId, maxRetries);
          throw;
        }
      }
    }
  }
}
