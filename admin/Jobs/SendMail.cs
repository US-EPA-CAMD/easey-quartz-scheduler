using System.Threading.Tasks;

using Quartz;
using Quartz.Job;

namespace Epa.Camd.Easey.JobScheduler.Jobs
{
  // There is no execute method becasue we are using the built in Quartz SendMailJob
  // However, this class was created to house the static StartNow function like we have for our custom jobs
  public class SendMail
  {
    public static async Task StartNow(
      IScheduler scheduler,
      string toEmail,
      string fromEmail,
      string subject,
      string message,
      string smtpHost,
      string smtpPort
    ) {
      IJobDetail job = JobBuilder.Create<SendMailJob>()
        .WithIdentity(
          Constants.JobDetails.SEND_EMAIL_KEY,
          Constants.JobDetails.SEND_EMAIL_GROUP
        )
        .UsingJobData("smtp_host", smtpHost)
        .UsingJobData("smtp_port", smtpPort)
        .Build();

      ITrigger trigger = TriggerBuilder.Create()
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