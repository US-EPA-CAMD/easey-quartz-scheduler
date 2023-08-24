using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;


namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class SubmissionReminderProcessQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }

    public static class SubmissionReminderProcessQueueIdentity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Submission Reminder Process Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if submissions reminder emails can be processed.";
      public static readonly string TriggerName = "Check file queue every minute for submission reminder emails";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in the queue which can be processed for Submission Reminders";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<SubmissionReminderProcessQueue>(WithSubmissionReminderProcessQueueIdentityJobKey(), SubmissionReminderProcessQueueIdentity.JobDescription);
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithSubmissionReminderProcessQueueIdentityJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_SUBMISSION_REMINDER_QUEUE_SCHEDULE"] ?? "0 0/1 * 1/1 * ? *";
        TriggerBuilder triggerBuilder = WithSubmissionReminderProcessQueueIdentityCronSchedule(cronExpression);

        if (await scheduler.CheckExists(jobKey)) {
          ITrigger trigger = await scheduler.GetTrigger(WithSubmissionReminderProcessQueueIdentityTriggerKey());

          if (
            trigger is ICronTrigger cronTrigger &&
            cronTrigger.CronExpressionString != cronExpression
          ) {
            await scheduler.RescheduleJob(WithSubmissionReminderProcessQueueIdentityTriggerKey(), triggerBuilder.Build());
            Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
          }
        } else {
          app.UseQuartzJob<SubmissionReminderProcessQueue>(triggerBuilder);
          Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
        }
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public SubmissionReminderProcessQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        List<EmailToProcess> inQueue = _dbContext.EmailToProcessQueue.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.email_to_process
            WHERE status_cd = 'QUEUED' AND email_type = 'submissionReminder'"
          ).ToList();

        HashSet<long> plantIdSet = new HashSet<long>();
        foreach(EmailToProcess process in inQueue){
          process.StatusCode = "WIP";
          _dbContext.EmailToProcessQueue.Update(process);
          plantIdSet.Add(Convert.ToInt64(process.FacId));
        }
        _dbContext.SaveChanges();

        //Create list of plantListIds
        long[] plantIdList = new long[plantIdSet.Count];
        plantIdSet.CopyTo(plantIdList);

        //Fire API Call
        ReminderNotificationPayload payload = new ReminderNotificationPayload();
        payload.plantIdList = plantIdList;
        payload.emailType = "submissionReminder";
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
        
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/support/email/emailRecipientList", httpContent); //TODO: Replace this with mocked result
        response.EnsureSuccessStatusCode();

        Console.WriteLine(response.Content.ReadAsStringAsync().Result);

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
        }

        //Load our to-send emails
        foreach(EmailToProcess process in inQueue){
          process.StatusCode = "COMPLETE";
          _dbContext.EmailToProcessQueue.Update(process);

          foreach(string emailTo in facIdToEmails[process.FacId]){
            EmailToSend es = new EmailToSend();

            es.Context = process.Context;
            es.StatusCode = "QUEUED";
            es.TemplateId = process.EventCode;
            es.ToEmail = emailTo;
            es.FromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_WINDOW_NOTIFICATION_FROM_EMAIL"];

            _dbContext.EmailToSend.Add(es);
          }
        }
        _dbContext.SaveChanges();

        return;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return;
      }
    }

    public static JobKey WithSubmissionReminderProcessQueueIdentityJobKey()
    {
      return new JobKey(SubmissionReminderProcessQueueIdentity.JobName, SubmissionReminderProcessQueueIdentity.Group);
    }

    public static TriggerKey WithSubmissionReminderProcessQueueIdentityTriggerKey()
    {
      return new TriggerKey(SubmissionReminderProcessQueueIdentity.TriggerName, SubmissionReminderProcessQueueIdentity.Group);
    }

    public static IJobDetail WithSubmissionReminderProcessQueueIdentityJobDetail()
    {
      return JobBuilder.Create<SubmissionReminderProcessQueue>()
          .WithIdentity(WithSubmissionReminderProcessQueueIdentityJobKey())
          .WithDescription(SubmissionReminderProcessQueueIdentity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithSubmissionReminderProcessQueueIdentityCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithSubmissionReminderProcessQueueIdentityTriggerKey())
          .WithDescription(SubmissionReminderProcessQueueIdentity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}