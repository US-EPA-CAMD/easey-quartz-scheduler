using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading;


namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class SubmissionJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Submission Job Queue";
      public static readonly string JobDescription = "Operates on an interval to determine if sets in SubmissionSet table can be submitted.";
      public static readonly string TriggerName = "Check submission queue every minute";
      public static readonly string TriggerDescription = "Operate every minute to determine if there are files in submission queue which can be triggered";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<SubmissionJobQueue>(WithJobKey(), Identity.JobDescription);
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_SUBMISSION_QUEUE_SCHEDULE"] ?? "0 0/1 * 1/1 * ? *";
        TriggerBuilder triggerBuilder = WithCronSchedule(cronExpression);

        if (await scheduler.CheckExists(jobKey)) {
          ITrigger trigger = await scheduler.GetTrigger(WithTriggerKey());

          if (
            trigger is ICronTrigger cronTrigger &&
            cronTrigger.CronExpressionString != cronExpression
          ) {
            await scheduler.RescheduleJob(WithTriggerKey(), triggerBuilder.Build());
            Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
          }
        } else {
          app.UseQuartzJob<SubmissionJobQueue>(triggerBuilder);
          Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
        }
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public SubmissionJobQueue(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");

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


        if(inWIP.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"])){
          if(inQueue.Count > 0){
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_SUBMISSION_JOBS"]) - inWIP.Count;
            Console.WriteLine("Scheduling Jobs: " + jobs_to_schedule);
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
                
                HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/submission/process", httpContent); //TODO: Replace this with mocked result

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

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

    public static TriggerKey WithTriggerKey()
    {
      return new TriggerKey(Identity.TriggerName, Identity.Group);
    }

    public static IJobDetail WithJobDetail()
    {
      return JobBuilder.Create<SubmissionJobQueue>()
          .WithIdentity(WithJobKey())
          .WithDescription(Identity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithTriggerKey())
          .WithDescription(Identity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}