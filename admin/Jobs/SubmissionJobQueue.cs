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
  public class SubmissionJobQueue : IJob
  {
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

        List<SubmissionSet> inQueue = _dbContext.SubmissionSet.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.submission_set
            WHERE status_cd = 'QUEUED'"
          ).ToList();

        List<SubmissionSet> inWIP = _dbContext.SubmissionSet.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.submission_set
            WHERE status_cd = 'WIP'"
          ).ToList();

        _logger.LogInformation("Found {InQueueCount} items in queue and {InWIPCount} items in WIP", inQueue?.Count ?? 0, inWIP?.Count ?? 0);

        if(inWIP.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"])){
          if(inQueue.Count > 0){
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"]) - inWIP.Count;
            _logger.LogInformation("Scheduling {JobsToSchedule} jobs", jobs_to_schedule);

            int index = 0;

            string clientToken = await Utils.generateClientToken();

            for(int i = 0; i < jobs_to_schedule; i++){
              if(index < inQueue.Count){
                inQueue[i].StatusCode = "WIP";
                _dbContext.SubmissionSet.Update(inQueue[i]);
                _dbContext.SaveChanges();

                _logger.LogInformation("Submitting to camd-services SubmissionSetId {SubmissionSetId}", inQueue[i]?.SetId);

                try
                {
                  await SubmitProcessJob(inQueue[i]?.SetId, clientToken);
                }
                catch
                {
                  inQueue[i].StatusCode = "ERROR";
                  _dbContext.SubmissionSet.Update(inQueue[i]);
                  _dbContext.SaveChanges();
                }

                Thread.Sleep(Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_SUBMISSION_JOB_QUEUE_DELAY"] ?? "1") * 1000);
                index++;
              }
            }
          }
          else
          {
             _logger.LogInformation("No items in queue to process.");
          }
        }
        else
        {
           _logger.LogInformation("Maximum number of submission jobs ({MaxJobs}) already in progress. Skipping processing...", Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"]);
        }

        return;
      }
      catch (Exception e)
      {
        _logger.LogError(e, "An error occurred while executing a submission job at {Time}", DateTimeOffset.Now);
        return;
      }
    }

    /// <summary>
    /// Submits a process job to the camd-services API with retry logic and exponential backoff.
    /// </summary>
    /// <param name="setId">The submission set ID to process.</param>
    /// <param name="clientToken">The client token for authentication.</param>
    private async Task SubmitProcessJob(string setId, string clientToken)
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
