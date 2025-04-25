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

                ToProcessSubmissionPayload payload = new ToProcessSubmissionPayload();
                payload.submissionSetId = inQueue[i].SetId;

                HttpClient client = new HttpClient();
                StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
                client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

                _logger.LogInformation("Submitting to camd-services SubmissionSetId {SubmissionSetId}", inQueue[i]?.SetId);
                HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/submission/process", httpContent); //TODO: Replace this with mocked result
                _logger.LogInformation("Submitted job for SubmissionSetId {SubmissionSetId} with response {ResponseStatusCode}", inQueue[i]?.SetId, response != null ? response.StatusCode.ToString() : "null");

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
  }
}
