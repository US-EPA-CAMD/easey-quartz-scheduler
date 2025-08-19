using System;
using System.Threading.Tasks;
using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class EmailQueue : IJob
  {
    private NpgSqlContext _dbContext = null;
    private readonly ILogger<EmailQueue> _logger;
    private readonly Guid _jobId = Guid.NewGuid();

    private IConfiguration Configuration { get; }

    public EmailQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<EmailQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    private async Task UpdateEmailStatus(EmailToSend emailToSend, string status)
    {
      emailToSend.StatusCode = status;
      _dbContext.EmailToSend.Update(emailToSend);
      await _dbContext.SaveChangesAsync();
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing EmailQueue job. JobId: {JobId}", _jobId);

      JobLog jl = new JobLog()
      {
        JobId = _jobId,
        JobSystem = "Quartz",
        JobClass = "EmailQueue",
        JobName = context.JobDetail.Key.Name,
        AddDate = Utils.getCurrentEasternTime(),
        StartDate = Utils.getCurrentEasternTime(),
        EndDate = null,
        StatusCd = "WIP"
      };
      _dbContext.JobLogs.Add(jl);
      await _dbContext.SaveChangesAsync();

      try
      {
        _logger.LogInformation("Checking Email Queue job now");

        List<EmailToSend> inQueue = _dbContext.EmailToSend.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.email_to_send
            WHERE status_cd = 'QUEUED'"
          ).ToList();

        List<EmailToSend> inWIP = _dbContext.EmailToSend.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.email_to_send
            WHERE status_cd = 'WIP'"
          ).ToList();

        string clientToken = await Utils.generateClientToken();
        int maxEmails = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_EMAILS_TO_SEND"]);

        if (inWIP.Count < maxEmails)
        {
          if (inQueue.Count > 0)
          {
            int jobs_to_schedule = Math.Min(maxEmails - inWIP.Count, inQueue.Count);

            _logger.LogInformation("Scheduling {JobCount} Email Queue jobs", jobs_to_schedule);

            for (int i = 0; i < jobs_to_schedule; i++)
            {
              EmailToSend emailToSend = inQueue[i];
              
              try
              {
                // Mark as WIP
                await UpdateEmailStatus(emailToSend, "WIP");

                // Call Camd-Service email service
                ToProcessPayload payload = new ToProcessPayload();
                payload.emailToSendId = Convert.ToInt64(emailToSend.SendId);

                HttpClient client = new HttpClient();
                StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
                client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

                HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/support/email/process", httpContent);
                
                bool emailProcessed = false;

                if (!response.IsSuccessStatusCode)
                {
                  _logger.LogError(
                    "Email service returned HTTP error for SendId: {SendId}. Status: {StatusCode}, Reason: {ReasonPhrase}",
                    emailToSend.SendId, response.StatusCode, response.ReasonPhrase ?? "No reason phrase");
                }
                else
                {
                  // Parse response to check actual processing status
                  string responseContent = await response.Content.ReadAsStringAsync();
                  EmailProcessResponse emailResponse = JsonConvert.DeserializeObject<EmailProcessResponse>(responseContent);

                  if (emailResponse != null && emailResponse.success)
                  {
                    emailProcessed = true;
                    _logger.LogInformation("Email successfully processed. SendId: {SendId}", emailToSend.SendId);
                  }
                  else
                  {
                    _logger.LogError("Email processing failed for SendId: {SendId}. Error: {ErrorMessage}", emailToSend.SendId, emailResponse?.message ?? "Unknown error");
                  }
                }
                
                // Revert to QUEUED if not successfully processed
                if (!emailProcessed)
                {
                  await UpdateEmailStatus(emailToSend, "QUEUED");
                  _logger.LogInformation("Reverted email to QUEUED status. SendId: {SendId}", emailToSend.SendId);
                }

                // Delay between email processing to avoid overwhelming the email service and maintain rate limiting
                Thread.Sleep(Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_EMAIL_QUEUE_DELAY"] ?? "1") * 1000);
              }
              catch (Exception ex)
              {
                _logger.LogError(ex, "Error processing email SendId: {SendId}", emailToSend?.SendId);
                // Revert to QUEUED on exception
                await UpdateEmailStatus(emailToSend, "QUEUED");
                _logger.LogInformation("Reverted email to QUEUED status due to exception. SendId: {SendId}", emailToSend?.SendId);
              }
            }
          }
        }

        // Mark job as complete
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();

        return;
      }
      catch (Exception e)
      {
        // Mark job as error
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogError(e, "Error executing EmailQueue job. JobId: {JobId}", _jobId);
        return;
      }
    }
  }
}
