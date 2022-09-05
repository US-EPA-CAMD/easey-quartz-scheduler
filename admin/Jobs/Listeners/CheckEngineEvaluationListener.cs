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
      string monPlanConfig = dataMap.GetString("Configuration");
      string userId = dataMap.GetString("UserId");
      string userEmail = dataMap.GetString("UserEmail");
      string submittedOn = dataMap.GetString("SubmittedOn");
      string evaluationStatus = dataMap.GetString("EvaluationStatus");
      string evaluationResult = dataMap.GetString("EvaluationResult");
      string fromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_EMAIL"];

      string subject = string.Format(
        "{0} Evaluation {1} {2} - {3}",
        CheckEngineEvaluation.GetProcess(processCode),
        facilityName,
        monPlanConfig,
        evaluationResult
      );

      string message = string.Format(@"
        The {0} Evaluation process submitted by {1} on {2} for...

        Facility Id: {3}
        Facility Name: {4}
        Configuration: {5}

        {6} with a status of {7}!
        
        You can view details of the report here...
        https://{8}/ecmps/workspace/monitoring-plans/{9}/evaluation-report

        Thanks,
        ECMPS Support",
        CheckEngineEvaluation.GetProcess(processCode),
        userId,
        submittedOn,
        facilityId,
        facilityName,
        monPlanConfig,
        evaluationResult.ToUpper(),
        evaluationStatus.ToUpper(),
        Configuration["EASEY_QUARTZ_SCHEDULER_HOST"],
        monitorPlanId
      );

      await SendMail.StartNow(
        userEmail,
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