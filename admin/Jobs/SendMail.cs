using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using Quartz.Job;
using SilkierQuartz;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  // There is no execute method becasue we are using the built in Quartz SendMailJob
  // However, this class was created to house the static StartNow function like we have for our custom jobs
  public class SendMail
  {
    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.QUARTZ;
      public static readonly string JobName = "Send Email";
      public static readonly string JobDescription = "Sends an email per the provided job data map.";
      public static readonly string TriggerName = "Sent email to {0} for {1}";
      public static readonly string TriggerDescription = JobDescription;
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<SendMailJob>(WithJobKey(), Identity.JobDescription);
    }

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

    public static TriggerKey WithTriggerKey(string toEmail, string purpose)
    {
      return new TriggerKey(string.Format(
        Identity.TriggerName, toEmail, purpose
      ),
      Identity.Group);
    }

    public static async Task StartNow(
      string toEmail,
      string fromEmail,
      string subject,
      string message,
      string purpose,
      IScheduler scheduler,
      IConfiguration configuration
    ) {
      string smtpHost = configuration["EASEY_QUARTZ_SCHEDULER_SMTP_HOST"];
      string smtpPort = configuration["EASEY_QUARTZ_SCHEDULER_SMTP_PORT"];

      IJobDetail job = JobBuilder.Create<SendMailJob>()
        .WithIdentity(WithJobKey())
        .WithDescription(Identity.JobDescription)
        .UsingJobData("smtp_host", smtpHost)
        .UsingJobData("smtp_port", smtpPort)
        .Build();

      ITrigger trigger = TriggerBuilder.Create()
        .WithIdentity(WithTriggerKey(toEmail, purpose))
        .WithDescription(Identity.TriggerDescription)
        .UsingJobData("recipient", toEmail)
        .UsingJobData("sender", fromEmail)
        .UsingJobData("subject", subject)
        .UsingJobData("message", message)
        .StartNow()
        .Build();

      await scheduler.ScheduleJob(job, trigger);
    }
  }
}