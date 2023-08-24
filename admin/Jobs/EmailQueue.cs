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
using System.Threading;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class EmailQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }

    public static class EmailQueueIdentity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Email Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if emails in the send email table can be sent.";
      public static readonly string TriggerName = "Check to_send email queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are emails in the queue which can be sent";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<EmailQueue>(WithEmailQueueJobKey(), EmailQueueIdentity.JobDescription);
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithEmailQueueJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_EMAIL_QUEUE_SCHEDULE"] ?? "0 0/1 * 1/1 * ? *";
        TriggerBuilder triggerBuilder = WithEmailQueueCronSchedule(cronExpression);

        if (await scheduler.CheckExists(jobKey)) {
          ITrigger trigger = await scheduler.GetTrigger(WithEmailQueueTriggerKey());

          if (
            trigger is ICronTrigger cronTrigger &&
            cronTrigger.CronExpressionString != cronExpression
          ) {
            await scheduler.RescheduleJob(WithEmailQueueTriggerKey(), triggerBuilder.Build());
            Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
          }
        } else {
          app.UseQuartzJob<EmailQueue>(triggerBuilder);
          Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
        }
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public EmailQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");

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

        if(inWIP.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_EMAILS_TO_SEND"])){
          if(inQueue.Count > 0){
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_EMAILS_TO_SEND"]) - inWIP.Count;
            Console.WriteLine("Scheduling Jobs: " + jobs_to_schedule);
            int index = 0;
            for(int i = 0; i < jobs_to_schedule; i++){
              if(index < inQueue.Count){
                //Call Camd-Service email service
                inQueue[i].StatusCode = "WIP";
                _dbContext.EmailToSend.Update(inQueue[i]);
                _dbContext.SaveChanges();

                ToProcessPayload payload = new ToProcessPayload();
                payload.emailToProcessId = Convert.ToInt64(inQueue[i].SendId);

                HttpClient client = new HttpClient();
                StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
                client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);
                
                HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/support/email/process", httpContent); //TODO: Replace this with mocked result

                Thread.Sleep(1000);
                index++;
              }
            }
          }
        }

        return;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return;
      }
    }

    public static JobKey WithEmailQueueJobKey()
    {
      return new JobKey(EmailQueueIdentity.JobName, EmailQueueIdentity.Group);
    }

    public static TriggerKey WithEmailQueueTriggerKey()
    {
      return new TriggerKey(EmailQueueIdentity.TriggerName, EmailQueueIdentity.Group);
    }

    public static IJobDetail WithEmailQueueJobDetail()
    {
      return JobBuilder.Create<EmailQueue>()
          .WithIdentity(WithEmailQueueJobKey())
          .WithDescription(EmailQueueIdentity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithEmailQueueCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithEmailQueueTriggerKey())
          .WithDescription(EmailQueueIdentity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}