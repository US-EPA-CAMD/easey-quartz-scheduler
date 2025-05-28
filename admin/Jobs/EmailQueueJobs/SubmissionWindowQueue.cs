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
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class SubmissionWindowProcessQueue : IJob
  {
    private NpgSqlContext _dbContext = null;
    private readonly ILogger<SubmissionWindowProcessQueue> _logger;
    private IConfiguration Configuration { get; }

    public SubmissionWindowProcessQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<SubmissionWindowProcessQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        List<EmailToProcess> inQueue = _dbContext.EmailToProcessQueue.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.email_to_process
            WHERE status_cd = 'QUEUED' AND email_type = 'submissionWindow'"
          ).ToList();

        HashSet<long> plantIdSet = new HashSet<long>();
        foreach(EmailToProcess process in inQueue){
          process.StatusCode = "WIP";
          _dbContext.EmailToProcessQueue.Update(process);
          plantIdSet.Add(Convert.ToInt64(process.FacId));
        }
        _dbContext.SaveChanges();

        //Comment out the call to camd-services/support/email/emailRecipientList because the endpoint does not yet exist.
        /*
        //Create list of plantListIds
        long[] plantIdList = new long[plantIdSet.Count];
        plantIdSet.CopyTo(plantIdList);


         
        //Fire API Call
        ReminderNotificationPayload payload = new ReminderNotificationPayload();
        payload.plantIdList = plantIdList;
        payload.emailType = "submissionWindow";
        payload.isMats = null;
        payload.plantId = null;
        payload.submissionType = null;
        payload.userId = null;

        
        HttpClient client = new HttpClient();
        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);

        string clientToken = await Utils.generateClientToken();     
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

        _logger.LogInformation("Sending POST request to /support/email/emailRecipientList.");
        
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/support/email/emailRecipientList", httpContent); //TODO: Replace this with mocked result
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Received successful response: {ResponseContent}", response.Content.ReadAsStringAsync().Result);

        RecipientResponse recipientResponse = JsonConvert.DeserializeObject<RecipientResponse>(response.Content.ReadAsStringAsync().Result);


        //Build a master list of facilityIds to userEmails [performance will be improved greatly in case of large process email set]
        Dictionary<decimal, HashSet<string>> facIdToEmails = new Dictionary<decimal, HashSet<string>>();
        foreach(Recipient r in recipientResponse.recipientList){
          foreach(decimal facId in r.plantIdList){
            if(facIdToEmails.ContainsKey(facId)){
              foreach(string emailAddress in r.emailAddressList){
                facIdToEmails[facId].Add(emailAddress);
              }
            }else{
              HashSet<string> emails = new HashSet<string>();
              foreach(string emailAddress in r.emailAddressList){
                emails.Add(emailAddress);
              }

              facIdToEmails.Add(facId, emails);
            }
          }
        } */

        //Load our to-send emails
        foreach(EmailToProcess process in inQueue){
          process.StatusCode = "COMPLETE";
          _dbContext.EmailToProcessQueue.Update(process);

          /*foreach(string emailTo in facIdToEmails[process.FacId]){
            EmailToSend es = new EmailToSend {
              Context = process.Context,
              StatusCode = "QUEUED",
              TemplateId = process.EventCode,
              ToEmail = emailTo,
              FromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_WINDOW_NOTIFICATION_FROM_EMAIL"]
            };

            _dbContext.EmailToSend.Add(es);
          }*/
        }
        _dbContext.SaveChanges();

        return;
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Error scheduling Submission Window Queue job");
        return;
      }
    }
  }
}
