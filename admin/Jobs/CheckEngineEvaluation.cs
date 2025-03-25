using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;

using DatabaseAccess;
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using ECMPS.Definitions.Extensions;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class CheckEngineEvaluation : IJob
  {
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }
    private readonly ILogger<CheckEngineEvaluation> _logger;
    static SemaphoreSlim semaphore;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.EVALUATIONS;
      public static readonly string JobName = "{0} Evaluation {1}";
      public static readonly string JobDescription = "Evaluates a {0} data set for accuracy as specified by the EPA Part 75 reporting instructions.";
      public static readonly string TriggerName = "{0} Evaluation ({1} {2}) {3}";
      public static readonly string TriggerDescription = "Evaluates a {0} data set for accuracy as specified by the EPA Part 75 reporting instructions.";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<CheckEngineEvaluation>(WithJobKey("MP"), Identity.JobDescription);
      services.AddQuartzJob<CheckEngineEvaluation>(WithJobKey("QA"), Identity.JobDescription);
      services.AddQuartzJob<CheckEngineEvaluation>(WithJobKey("EM"), Identity.JobDescription);
    }

    public CheckEngineEvaluation(
      NpgSqlContext dbContext,
      IConfiguration configuration,
      ILogger<CheckEngineEvaluation> logger
    )
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    private EvalStatusCode getStatusCodeByCheckId(string checkSessionId, bool result){

      if(result){
        CheckSession chkSession = _dbContext.CheckSessions.Find(checkSessionId);
        SeverityCode severity = _dbContext.SeverityCodes.Find(chkSession.SeverityCode);
        return _dbContext.EvalStatusCodes.Find(severity.EvalStatusCode);
      }
      else{
        return _dbContext.EvalStatusCodes.Find("ERR");
      }
    }

    public Task Execute(IJobExecutionContext context)
    {
        string instanceIndex = Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX") ?? "unknown";
        
        // Initialize evaluation stages
        List<EvaluationStageDto> evaluationStages = new List<EvaluationStageDto>
        {
            new EvaluationStageDto { action = "EVAL_STARTED", dateTime = DateTime.UtcNow.ToString("o") }
        };

        JobDataMap dataMap = context.MergedJobDataMap;
        JobKey key = context.JobDetail.Key;

        string id = dataMap.GetString("Id");
        _logger.LogInformation("[Instance {InstanceIndex}] Starting evaluation ID: {EvalId}", instanceIndex, id);

        Evaluation evalRecord = _dbContext.Evaluations.Find(Int64.Parse(id));

        evalRecord.StatusCode = "WIP";
        evalRecord.StartedTime = Utils.getCurrentEasternTime();
        _dbContext.Evaluations.Update(evalRecord);
        _dbContext.SaveChanges();

        evaluationStages.Add(new EvaluationStageDto
        {
            action = "EVAL_RECORD_UPDATED",
            dateTime = DateTime.UtcNow.ToString("o")
        });

        string processCode = dataMap.GetString("ProcessCode");
        int facilityId = dataMap.GetIntValue("FacilityId");
        string facilityName = dataMap.GetString("FacilityName");
        string monitorPlanId = dataMap.GetString("MonitorPlanId");
        string monPlanConfig = dataMap.GetString("Configuration");
        string userId = dataMap.GetString("UserId");
        string userEmail = dataMap.GetString("UserEmail");
        string queuedTime = dataMap.GetString("QueuedTime");

        EvaluationSet es = null;

        try
        {
            string connectionString = ConnectionStringManager.getConnectionString(Configuration);
            int commandTimeout = Configuration.GetValue<int>("EASEY_DB_STATEMENT_TIMEOUT", 300);

                _logger.LogInformation(
                "[Instance {InstanceIndex}] Executing {Group}.{Name}",
                instanceIndex,
                key.Group,
                key.Name,
                new LogVariable("Id", id),
                new LogVariable("Process Code", processCode),
                new LogVariable("Facility Id", facilityId),
                new LogVariable("Facility Name", facilityName),
                new LogVariable("Monitor Plan Id", monitorPlanId),
                new LogVariable("Configuration", monPlanConfig),
                new LogVariable("User Id", userId),
                new LogVariable("User Email", userEmail),
                new LogVariable("Queued Time", queuedTime)
            );

            string dllPath = Configuration["EASEY_QUARTZ_SCHEDULER_CHECK_ENGINE_DLL_PATH"];
            cCheckEngine checkEngine = new cCheckEngine(userId, connectionString, dllPath, "dumpfilePath", commandTimeout);

            MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);

            string evaluationStatus = "";

            es = _dbContext.EvaluationSet.Find(dataMap.GetString("SetId"));

            switch (processCode)
            {
                case "MP":
                    _logger.LogInformation("[Instance {InstanceIndex}] Running MP checks for evaluation {EvalId}", instanceIndex, id);

                    mp.EvalStatus = "WIP";
                    _dbContext.MonitorPlans.Update(mp);
                    _dbContext.SaveChanges();

                    bool mpResult = checkEngine.RunChecks_MpReport(monitorPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, es.SetId);
                    _logger.LogInformation("[Instance {InstanceIndex}] MP checks completed for evaluation {EvalId} with result: {Result}", 
                        instanceIndex, id, mpResult);

                    if (!mpResult)
                    {
                        string exMessage = "MP Report Check Run Failed.";

                        if (!checkEngine.CheckEngineErrors.IsWhitespace())
                        {
                            exMessage += Environment.NewLine + Environment.NewLine + checkEngine.CheckEngineErrors;
                        }

                        throw new Exception(exMessage);
                    }

                    _dbContext.Entry<MonitorPlan>(mp).Reload();
                    EvalStatusCode evalStatus = getStatusCodeByCheckId(mp.CheckSessionId, mpResult);
                    mp.EvalStatus = evalStatus.Code;
                    evaluationStatus = evalStatus.Code;
                    _dbContext.MonitorPlans.Update(mp);
                    context.MergedJobDataMap.Add("EvaluationStatus", evalStatus.Description);

                    _logger.LogInformation("[Instance {InstanceIndex}] Checking for QA evaluations for set {SetId}", 
                        instanceIndex, es.SetId);
                    List<Evaluation> qaEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT *
                        FROM camdecmpsaux.evaluation_queue
                        WHERE process_cd = 'QA' AND evaluation_set_id = {0}
                    ", es.SetId).ToList();
                    if(qaEvals.Count > 0){
                        _logger.LogInformation("[Instance {InstanceIndex}] Found {Count} QA evaluations to queue", 
                            instanceIndex, qaEvals.Count);
                        foreach(Evaluation e in qaEvals){
                            e.StatusCode = "QUEUED";
                            _dbContext.Evaluations.Update(e);
                        }
                    } else {
                        _logger.LogInformation("[Instance {InstanceIndex}] Checking for EM evaluations for monitor plan {MonPlanId}", 
                            instanceIndex, es.MonPlanId);
                        List<Evaluation> emEvals = _dbContext.Evaluations.FromSqlRaw(@"
                            SELECT eq.*
                            FROM camdecmpsaux.evaluation_queue eq
                            JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                            WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                            ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                        if(emEvals.Count >= 1){
                            _logger.LogInformation("[Instance {InstanceIndex}] Queueing first EM evaluation", instanceIndex);
                            emEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
                            _dbContext.Evaluations.Update(emEvals[0]);
                        }
                    }
                    // --------

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "RUN_CHECK_MP_REPORT_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });

                    _logger.LogInformation("RunChecks_MpReport returned a result of {Result}!", mpResult);
                    break;

                case "QA":
                    _logger.LogInformation("[Instance {InstanceIndex}] Starting QA checks for evaluation {EvalId}", instanceIndex, id);
                    if(!string.IsNullOrWhiteSpace(dataMap.GetString("testSumId"))){
                        string testId = dataMap.GetString("testSumId");
                        _logger.LogInformation("[Instance {InstanceIndex}] Processing test summary {TestId}", 
                            instanceIndex, testId);
                        TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
                        testSummaryRecord.EvalStatus = "WIP";
                        _dbContext.TestSummaries.Update(testSummaryRecord);

                        bool listResult = checkEngine.RunChecks_QaReport_Test(testId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("[Instance {InstanceIndex}] Test summary checks completed with result: {Result}", 
                            instanceIndex, listResult);

                        if (!listResult)
                        {
                            string exMessage = "QAT Report Check Run Failed.";

                            if (!checkEngine.CheckEngineErrors.IsWhitespace())
                            {
                                exMessage += Environment.NewLine + Environment.NewLine + checkEngine.CheckEngineErrors;
                            }

                            throw new Exception(exMessage);
                        }

                            _dbContext.Entry<TestSummary>(testSummaryRecord).Reload();
                        EvalStatusCode testSumEvalStatus = getStatusCodeByCheckId(testSummaryRecord.CheckSessionId, listResult);
                        evaluationStatus = testSumEvalStatus.Code;
                        testSummaryRecord.EvalStatus = testSumEvalStatus.Code;
                        _dbContext.TestSummaries.Update(testSummaryRecord);
                    }
                    else if(!string.IsNullOrWhiteSpace(dataMap.GetString("qaCertId"))){
                        string certId = dataMap.GetString("qaCertId");
                        _logger.LogInformation("[Instance {InstanceIndex}] Processing QA certification {CertId}", 
                            instanceIndex, certId);
                        CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
                        certIdRecord.EvalStatus = "WIP";
                        _dbContext.CertEvents.Update(certIdRecord);
                        _dbContext.SaveChanges();

                        bool listResult = checkEngine.RunChecks_QaReport_Qce(certId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("[Instance {InstanceIndex}] QA certification checks completed with result: {Result}", 
                            instanceIndex, listResult);

                        if (!listResult)
                        {
                            string exMessage = "QCE Report Check Run Failed.";

                            if (!checkEngine.CheckEngineErrors.IsWhitespace())
                            {
                                exMessage += Environment.NewLine + Environment.NewLine + checkEngine.CheckEngineErrors;
                            }

                            throw new Exception(exMessage);
                        }

                            _dbContext.Entry<CertEvent>(certIdRecord).Reload();
                        EvalStatusCode certEvalStatus = getStatusCodeByCheckId(certIdRecord.CheckSessionId, listResult);
                        evaluationStatus = certEvalStatus.Code;
                        certIdRecord.EvalStatus = certEvalStatus.Code;
                        _dbContext.CertEvents.Update(certIdRecord);
                    }
                    else{
                        string extensionExemptionId = dataMap.GetString("testExtensionExemption");
                        _logger.LogInformation("[Instance {InstanceIndex}] Processing test extension exemption {ExemptionId}", 
                            instanceIndex, extensionExemptionId);
                        TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
                        extensionExemptionRecord.EvalStatus = "WIP";
                        _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
                        _dbContext.SaveChanges();

                        bool listResult = checkEngine.RunChecks_QaReport_Tee(extensionExemptionId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("[Instance {InstanceIndex}] Extension exemption checks completed with result: {Result}", 
                            instanceIndex, listResult);

                        if (!listResult)
                        {
                            string exMessage = "TEE Report Check Run Failed.";

                            if (!checkEngine.CheckEngineErrors.IsWhitespace())
                            {
                                exMessage += Environment.NewLine + Environment.NewLine + checkEngine.CheckEngineErrors;
                            }

                            throw new Exception(exMessage);
                        }

                            _dbContext.Entry<TestExtensionExemption>(extensionExemptionRecord).Reload();
                        EvalStatusCode teeEvalStatus = getStatusCodeByCheckId(extensionExemptionRecord.CheckSessionId, listResult);
                        evaluationStatus = teeEvalStatus.Code;
                        extensionExemptionRecord.EvalStatus = teeEvalStatus.Code;
                        _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
                    }

                    _logger.LogInformation("[Instance {InstanceIndex}] Checking for EM evaluations after QA for monitor plan {MonPlanId}", 
                        instanceIndex, es.MonPlanId);
                    List<Evaluation> qaEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(qaEmEvals.Count >= 1){
                        _logger.LogInformation("[Instance {InstanceIndex}] Queueing first EM evaluation after QA", instanceIndex);
                        qaEmEvals[0].StatusCode = "QUEUED";
                        _dbContext.Evaluations.Update(qaEmEvals[0]);
                    }

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "IMPORT_CHECKS_QA_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });
                    _logger.LogInformation("[Instance {InstanceIndex}] QA import checks finished", instanceIndex);

                    break;

                case "EM":
                    int rptPeriodId = Int32.Parse(dataMap.GetString("rptPeriodId"));
                    _logger.LogInformation("[Instance {InstanceIndex}] Starting EM checks for period {PeriodId}", 
                        instanceIndex, rptPeriodId);
                    ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);

                    List<Evaluation> otherEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd IN ('PENDING', 'QUEUED', 'WIP') AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(otherEmEvals[0].RptPeriod != rptPeriodId){
                        _logger.LogInformation("[Instance {InstanceIndex}] Earlier EM evaluation exists, setting status to PENDING", instanceIndex);
                        evalRecord.StatusCode = "PENDING";
                        _dbContext.Evaluations.Update(evalRecord);
                        _dbContext.SaveChanges();
                        return Task.CompletedTask;
                    }


                    EmissionEvaluation emissionEvalRecord = _dbContext.EmissionEvaluations.Find(monitorPlanId, rptPeriodId); //TODO LOOK UP COMPOSITE PRIMARY KEY
                    emissionEvalRecord.EvalStatus = "WIP";
                    _dbContext.EmissionEvaluations.Update(emissionEvalRecord);
                    _dbContext.SaveChanges();

                    bool evalResult = checkEngine.RunChecks_EmReport(monitorPlanId, rptPeriodId, eCheckEngineRunMode.Normal, es.SetId);
                    _logger.LogInformation("[Instance {InstanceIndex}] EM checks completed with result: {Result}", 
                        instanceIndex, evalResult);

                    if (!evalResult) {
                        string exMessage = "EM Report Check Run Failed.";

                        if (!checkEngine.CheckEngineErrors.IsWhitespace())
                        {
                            exMessage += Environment.NewLine + Environment.NewLine + checkEngine.CheckEngineErrors;
                        }

                        throw new Exception(exMessage);
                    }

                    _dbContext.Entry<EmissionEvaluation>(emissionEvalRecord).Reload();
                    EvalStatusCode emissionEvalStatus = getStatusCodeByCheckId(emissionEvalRecord.CheckSessionId, evalResult);
                    evaluationStatus = emissionEvalStatus.Code;
                    emissionEvalRecord.EvalStatus = evaluationStatus;
                    _dbContext.EmissionEvaluations.Update(emissionEvalRecord);

                    _dbContext.ExecuteEmissionRefreshProcedure(monitorPlanId, rp.year, rp.quarter);

                    _logger.LogInformation("[Instance {InstanceIndex}] Checking for remaining EM evaluations", instanceIndex);
                    List<Evaluation> remainingEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(remainingEmEvals.Count >= 1){
                        _logger.LogInformation("[Instance {InstanceIndex}] Queueing next EM evaluation", instanceIndex);
                        remainingEmEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
                        _dbContext.Evaluations.Update(remainingEmEvals[0]);
                    }

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "EM_EVAL_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });

                    break;
                default:
                    throw new Exception("A Process Code of [MP, QA-QCE, QA-TEE, EM] is required and was not provided");
            }

            context.MergedJobDataMap.Add("EvaluationResult", "COMPLETED");

            evaluationStages.Add(new EvaluationStageDto
            {
                    action = "EVAL_COMPLETED",
                    dateTime = DateTime.UtcNow.ToString("o")
            });

            // Update our queued record
            evalRecord.StatusCode = "COMPLETE";
            evalRecord.CompletedTime = Utils.getCurrentEasternTime();
            evalRecord.EvalStatusCode = evaluationStatus;
            _dbContext.Evaluations.Update(evalRecord);
            _dbContext.SaveChanges();

            _logger.LogInformation("[Instance {InstanceIndex}] Evaluation {EvalId} completed successfully with status {Status}", 
                instanceIndex, id, evaluationStatus);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError("[Instance {InstanceIndex}] Evaluation {EvalId} failed with error: {ErrorMessage}", 
                instanceIndex, id, ex.Message);
            evalRecord.Details = JsonConvert.SerializeObject(ex);

            evalRecord.StatusCode = "ERROR";
            evalRecord.Note = ex.Message;
            evalRecord.NoteTime = Utils.getCurrentEasternTime();
            _dbContext.Evaluations.Update(evalRecord);
            _dbContext.SaveChanges();

            context.MergedJobDataMap.Add("EvaluationResult", "FAILED");
            context.MergedJobDataMap.Add("EvaluationStatus", "FATAL");
            _logger.LogError(ex.ToString());


        switch(processCode){ //Reset status codes to EVAL in case of an evaluation error
                case "MP":
                    _logger.LogInformation("[Instance {InstanceIndex}] Resetting MP evaluation status to EVAL", instanceIndex);
                    MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);
                    mp.EvalStatus = "EVAL";
                    _dbContext.MonitorPlans.Update(mp);
                    break;
                case "QA":
                    if(!string.IsNullOrWhiteSpace(dataMap.GetString("testSumId"))){
                        string testId = dataMap.GetString("testSumId");
                        _logger.LogInformation("[Instance {InstanceIndex}] Resetting test summary {TestId} status to EVAL", 
                            instanceIndex, testId);
                        TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
                        testSummaryRecord.EvalStatus = "EVAL";
                        _dbContext.TestSummaries.Update(testSummaryRecord);
                    }
                    else if(!string.IsNullOrWhiteSpace(dataMap.GetString("qaCertId"))){
                        string certId = dataMap.GetString("qaCertId");
                        _logger.LogInformation("[Instance {InstanceIndex}] Resetting QA certification {CertId} status to EVAL", 
                            instanceIndex, certId);
                        CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
                        certIdRecord.EvalStatus = "EVAL";
                        _dbContext.CertEvents.Update(certIdRecord);
                    }
                    else{
                        string extensionExemptionId = dataMap.GetString("testExtensionExemption");
                        _logger.LogInformation("[Instance {InstanceIndex}] Resetting extension exemption {ExemptionId} status to EVAL", 
                            instanceIndex, extensionExemptionId);
                        TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
                        extensionExemptionRecord.EvalStatus = "EVAL";
                        _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
                    }
                    break;
                case "EM":
                    int rptPeriodId = Int32.Parse(dataMap.GetString("rptPeriodId"));
                    _logger.LogInformation("[Instance {InstanceIndex}] Resetting EM evaluation status to EVAL for period {PeriodId}",
                                            instanceIndex, rptPeriodId);
                    ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);
                    EmissionEvaluation emissionEvalRecord = _dbContext.EmissionEvaluations.Find(monitorPlanId, rptPeriodId);
                    emissionEvalRecord.EvalStatus = "EVAL";
                    _dbContext.EmissionEvaluations.Update(emissionEvalRecord);
                    break;
            }
            _dbContext.SaveChanges();

        // Send the error email
            _ = SendEvaluationErrorEmail(ex.Message, es.SetId, evalRecord.EvaluationId, evaluationStages);

            return Task.FromException(ex);
        }
    }

    private async Task SendEvaluationErrorEmail(string rootError, string evaluationSetId, long evaluationId, List<EvaluationStageDto> evaluationStages)
    {
        try
        {
            var client = new HttpClient();
            // Populate request payload
            var payload = new
            {
                evaluationSetId = evaluationSetId,
                evaluationId = evaluationId,
                rootError = rootError,
                evaluationStages = evaluationStages
            };

            var httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

            // Set up headers
            client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
            client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);

            string clientToken = await Utils.generateClientToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

            // Make the HTTP POST request
            HttpResponseMessage response = await client.PostAsync($"{Configuration["EASEY_CAMD_SERVICES"]}/email/eval-error", httpContent);

            // Log the response
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Evaluation error email sent successfully for EvaluationSet ID: {EvaluationSetId}", evaluationSetId);
            }
            else
            {
                _logger.LogError("Failed to send evaluation error email. Status Code: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error sending evaluation error email: {ErrorMessage}", e.Message);
        }
    }


    public static string GetProcess(string processCode)
    {
      switch(processCode)
      {
        case "MP": return "Monitor Plan";
        case "QA": return "QA-Test Certification";
        case "EM": return "Emissions";
      }

      return null;
    }

    public static JobKey WithJobKey(string processCode)
    {
      return new JobKey(string.Format(
          Identity.JobName,
          GetProcess(processCode),
          Guid.NewGuid().ToString()
        ),
        Identity.Group
      );
    }

    public static TriggerKey WithTriggerKey(string processCode, string facilityName, string configuration)
    {
        return new TriggerKey(string.Format(
            Identity.TriggerName,
            GetProcess(processCode),
            facilityName,
            configuration,
            Guid.NewGuid().ToString()
          ),
          Identity.Group
        );
    }

    public static async Task StartNow(
      IScheduler scheduler,
      long id,
      string setId,
      string processCode,
      int facilityId,
      string facilityName,
      string monitorPlanId,
      string monPlanConfig,
      string userId,
      string userEmail,
      DateTime queuedTime,
      string testSumId,
      string qaCertEventId,
      string teeId,
      int? rptPeriod
    ) {
      string processName = GetProcess(processCode);

      IJobDetail job = JobBuilder.Create<CheckEngineEvaluation>()
        .WithIdentity(WithJobKey(processCode))
        .WithDescription(string.Format(Identity.JobDescription, processName))
        .UsingJobData("ProcessCode", processCode)
        .Build(); //

      ITrigger trigger = TriggerBuilder.Create()
        .WithIdentity(WithTriggerKey(processCode, facilityName, monPlanConfig))
        .WithDescription(string.Format(Identity.TriggerDescription, processName, facilityName, monPlanConfig))
        .UsingJobData("Id", id.ToString())
        .UsingJobData("SetId", setId)
        .UsingJobData("FacilityId", facilityId)
        .UsingJobData("FacilityName", facilityName)
        .UsingJobData("MonitorPlanId", monitorPlanId)
        .UsingJobData("Configuration", monPlanConfig)
        .UsingJobData("UserId", userId)
        .UsingJobData("UserEmail", userEmail)        
        .UsingJobData("QueuedTime", queuedTime.ToString())
        .UsingJobData("qaCertId", qaCertEventId)
        .UsingJobData("testExtensionExemption", teeId)
        .UsingJobData("testSumId", testSumId)
        .UsingJobData("rptPeriodId", rptPeriod.ToString())        
        .StartNow()
        .Build();

      await scheduler.ScheduleJob(job, trigger);
    }
  }
}
