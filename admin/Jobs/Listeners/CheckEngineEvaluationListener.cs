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
      string fromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_EMAIL"];

      string subject = string.Format(
        "{0} Evaluation {1} {2} - COMPLETE",
        CheckEngineEvaluation.GetProcess(processCode),
        facilityName,
        monPlanConfig
      );

      string message = string.Format(@"
        {0} Evaluation for the following details has completed successfully!

        Facility Id: {1}          Facility Name: {2}
        Configuration: {3}        Submitted: {4}
        Status: {5}               Report Url: https://{6}/ecmps/monitoring-plans/{7}/evaluation-report

        Thanks,
        ECMPS Support",
        CheckEngineEvaluation.GetProcess(processCode),
        facilityId,
        facilityName,
        monPlanConfig,
        submittedOn,
        evaluationStatus,
        Configuration["EASEY_QUARTZ_SCHEDULER_HOST"],
        monitorPlanId
      );;

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