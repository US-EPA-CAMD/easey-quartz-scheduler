using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Quartz;
using Quartz.Listener;

namespace Epa.Camd.Quartz.Scheduler.Jobs.Listeners
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
      JobKey key = context.JobDetail.Key;
      JobDataMap dataMap = context.MergedJobDataMap;

      string id = dataMap.GetString("Id");
      string processCode = dataMap.GetString("ProcessCode");
      int facilityId = dataMap.GetIntValue("FacilityId");
      string facilityName = dataMap.GetString("FacilityName");
      string monitorPlanId = dataMap.GetString("MonitorPlanId");
      string configuration = dataMap.GetString("Configuration");
      string userId = dataMap.GetString("UserId");
      string submittedOn = dataMap.GetString("SubmittedOn");

      string toEmail = "jasonwhitehead@cvpcorp.com";//dataMap.GetString("UserEmail");
      string fromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_EMAIL"];

      string subject = string.Format(
        "{0} Evaluation of {1} {2} Completed",
        CheckEngineEvaluation.GetProcess(processCode),
        facilityName,
        configuration
      );

      string message = string.Format(
        "{0} Evaluation of {1} {2} completed with an Evaluation Status of {3}",
        CheckEngineEvaluation.GetProcess(processCode),
        facilityName,
        configuration,
        "PASS"
      );;

      await SendMail.StartNow(
        toEmail,
        fromEmail,
        subject,
        message,
        subject,
        context.Scheduler,
        Configuration
      );

      await base.JobWasExecuted(context, jobException, cancellationToken);
    }
  }
}