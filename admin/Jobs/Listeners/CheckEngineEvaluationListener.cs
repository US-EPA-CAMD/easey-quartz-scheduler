using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Quartz;
using Quartz.Listener;

namespace Epa.Camd.Easey.JobScheduler.Jobs.Listeners
{
  public class CheckEngineEvaluationListener : JobListenerSupport
  {
    public override string Name => "Check Engine Evaluation Listener";

    private IConfiguration Configuration { get; }

    public CheckEngineEvaluationListener(IConfiguration configuration)
    {
      Configuration = configuration;
    }    

    public override async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
    {
      //TODO: get userId from context and use CDX to get user email address
      string toEmail = "jasonwhitehead@cvpcorp.com";
      string fromEmail = "jwhitehead@austin.rr.com";//Configuration["EASEY_QUARTZ_EMAIL"];
      //TODO: confirm subject line
      string subject = "Check Engine Evaluation Status";
      //TODO: confirm message format & add link to eval report
      string message = "This is a test of the Monitor Plan evaluation status email";
      string smtpHost = "smtp-server.austin.rr.com";//Configuration["EASEY_QUARTZ_SMTP_HOST"];
      string smtpPort = "587";//Configuration["EASEY_QUARTZ_SMTP_PORT"];

      await SendMail.StartNow(
        context.Scheduler,
        toEmail,
        fromEmail,
        subject,
        message,
        smtpHost,
        smtpPort
      );

      await base.JobWasExecuted(context, jobException, cancellationToken);
    }
  }
}