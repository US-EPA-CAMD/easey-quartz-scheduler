using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

using DatabaseAccess;
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using ECMPS.Definitions.Extensions;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class CheckEngineEvaluation : IJob
  {
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }
    private readonly ILogger<CheckEngineEvaluation> _logger;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.EVALUATIONS;
      public static readonly string JobName = "{0} Evaluation {1}";
      public static readonly string JobDescription = "Evaluates a {0} data set for accuracy as specified by the EPA Part 75 reporting instructions.";
      public static readonly string TriggerName = "{0} Evaluation ({1} {2}) {3}";
      public static readonly string TriggerDescription = "Evaluates a {0} data set for accuracy as specified by the EPA Part 75 reporting instructions.";
    }

    /// <summary>
    /// Custom exception for errors emitted during check engine evaluations.
    /// </summary>
    public class CheckEngineException : Exception
    {
      public CheckEngineException(string message) : base(message) { }
      public CheckEngineException(string message, Exception innerException) : base(message, innerException) { }
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

    public async Task Execute(IJobExecutionContext context)
    {
        // Initialize evaluation stages
        List<EvaluationStageDto> evaluationStages = new List<EvaluationStageDto>
        {
            new EvaluationStageDto { action = "EVAL_STARTED", dateTime = DateTime.UtcNow.ToString("o") }
        };

        JobDataMap dataMap = context.MergedJobDataMap;
        JobKey key = context.JobDetail.Key;

        string id = dataMap.GetString("Id");
        _logger.LogInformation("Starting evaluation ID: {EvalId}", id);

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

        cCheckEngine checkEngine = null;
        EvaluationSet es = null;

        try
        {
            string connectionString = ConnectionStringManager.getConnectionString(Configuration);
            int commandTimeout = Configuration.GetValue<int>("EASEY_DB_COMMAND_TIMEOUT", 300);

            _logger.LogInformation(
                "Executing {Group}.{Name} | EvalId: {EvalId}, Process Code: {ProcessCode}, Facility Id: {FacilityId}, Facility Name: {FacilityName}, Monitor Plan Id: {MonitorPlanId}, Configuration: {Configuration}, User Id: {UserId}, Queued Time: {QueuedTime}",
                key.Group,
                key.Name,
                id,
                processCode,
                facilityId,
                facilityName,
                monitorPlanId,
                monPlanConfig,
                userId,
                queuedTime
            );


            string dllPath = Configuration["EASEY_QUARTZ_SCHEDULER_CHECK_ENGINE_DLL_PATH"];
            
            // Ensure evaluation ID is not null for evaluation checks
            if (evalRecord.EvaluationId == 0)
            {
                throw new InvalidOperationException($"Evaluation ID is required but was null for evaluation record ID: {id}");
            }
            
            checkEngine = new cCheckEngine(userId, connectionString, dllPath, "dumpfilePath", commandTimeout, evalRecord.EvaluationId);
            
            _logger.LogInformation("Check engine initialized with EvalId: {EvalId}", evalRecord.EvaluationId);

            MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);

            string evaluationStatus = "";

            es = _dbContext.EvaluationSet.Find(dataMap.GetString("SetId"));

            switch (processCode)
            {
                case "MP":
                    _logger.LogInformation("Running MP checks for evaluation {EvalId}", id);

                    mp.EvalStatus = "WIP";
                    _dbContext.MonitorPlans.Update(mp);
                    _dbContext.SaveChanges();

                    bool mpResult = checkEngine.RunChecks_MpReport(monitorPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, es.SetId);
                    _logger.LogInformation("MP checks completed for evaluation {EvalId} with result: {Result}", id, mpResult);

                    if (!mpResult)
                    {
                        throw new CheckEngineException("MP Report Check Run Failed.");
                    }

                    _dbContext.Entry<MonitorPlan>(mp).Reload();
                    EvalStatusCode evalStatus = getStatusCodeByCheckId(mp.CheckSessionId, mpResult);
                    mp.EvalStatus = evalStatus.Code;
                    evaluationStatus = evalStatus.Code;
                    _dbContext.MonitorPlans.Update(mp);
                    context.MergedJobDataMap.Add("EvaluationStatus", evalStatus.Description);

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "RUN_CHECK_MP_REPORT_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });

                    _logger.LogInformation("RunChecks_MpReport returned a result of {Result}, EvalId: {EvalId}!", mpResult, id);
                    break;

                case "QA":
                    _logger.LogInformation("Starting QA checks for evaluation {EvalId}", id);
                    if(!string.IsNullOrWhiteSpace(dataMap.GetString("testSumId"))){
                        string testId = dataMap.GetString("testSumId");
                        _logger.LogInformation("Processing test summary {TestId}, EvalId: {EvalId}", 
                            testId, id);
                        TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
                        testSummaryRecord.EvalStatus = "WIP";
                        _dbContext.TestSummaries.Update(testSummaryRecord);
                        _dbContext.SaveChanges();

                        bool listResult = checkEngine.RunChecks_QaReport_Test(testId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("Test summary checks completed with result: {Result}, EvalId: {EvalId}", 
                            listResult, id);

                        if (!listResult)
                        {
                            throw new CheckEngineException("QAT Report Check Run Failed.");
                        }

                            _dbContext.Entry<TestSummary>(testSummaryRecord).Reload();
                        EvalStatusCode testSumEvalStatus = getStatusCodeByCheckId(testSummaryRecord.CheckSessionId, listResult);
                        evaluationStatus = testSumEvalStatus.Code;
                        testSummaryRecord.EvalStatus = testSumEvalStatus.Code;
                        _dbContext.TestSummaries.Update(testSummaryRecord);
                    }
                    else if(!string.IsNullOrWhiteSpace(dataMap.GetString("qaCertId"))){
                        string certId = dataMap.GetString("qaCertId");
                        _logger.LogInformation("Processing QA certification {CertId}, EvalId: {EvalId}", 
                            certId, id);
                        CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
                        certIdRecord.EvalStatus = "WIP";
                        _dbContext.CertEvents.Update(certIdRecord);
                        _dbContext.SaveChanges();

                        bool listResult = checkEngine.RunChecks_QaReport_Qce(certId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("QA certification checks completed with result: {Result}, EvalId: {EvalId}", 
                            listResult, id);

                        if (!listResult)
                        {
                            throw new CheckEngineException("QCE Report Check Run Failed.");
                        }

                            _dbContext.Entry<CertEvent>(certIdRecord).Reload();
                        EvalStatusCode certEvalStatus = getStatusCodeByCheckId(certIdRecord.CheckSessionId, listResult);
                        evaluationStatus = certEvalStatus.Code;
                        certIdRecord.EvalStatus = certEvalStatus.Code;
                        _dbContext.CertEvents.Update(certIdRecord);
                    }
                    else{
                        string extensionExemptionId = dataMap.GetString("testExtensionExemption");
                        _logger.LogInformation("Processing test extension exemption {ExemptionId}, EvalId: {EvalId}", 
                            extensionExemptionId, id);
                        TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
                        extensionExemptionRecord.EvalStatus = "WIP";
                        _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
                        _dbContext.SaveChanges();

                        bool listResult = checkEngine.RunChecks_QaReport_Tee(extensionExemptionId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);
                        _logger.LogInformation("Extension exemption checks completed with result: {Result}, EvalId: {EvalId}", 
                            listResult, id);

                        if (!listResult)
                        {
                            throw new CheckEngineException("TEE Report Check Run Failed.");
                        }

                            _dbContext.Entry<TestExtensionExemption>(extensionExemptionRecord).Reload();
                        EvalStatusCode teeEvalStatus = getStatusCodeByCheckId(extensionExemptionRecord.CheckSessionId, listResult);
                        evaluationStatus = teeEvalStatus.Code;
                        extensionExemptionRecord.EvalStatus = teeEvalStatus.Code;
                        _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
                    }

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "IMPORT_CHECKS_QA_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });
                    _logger.LogInformation("QA import checks finished, EvalId: {EvalId}", id);

                    break;

                case "EM":
                    int rptPeriodId = Int32.Parse(dataMap.GetString("rptPeriodId"));
                    _logger.LogInformation("Starting EM checks for period {PeriodId}, EvalId: {EvalId}", 
                        rptPeriodId, id);
                    ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);

                    List<Evaluation> otherEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd IN ('PENDING', 'QUEUED', 'CLAIMED', 'WIP') AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(otherEmEvals[0].RptPeriod != rptPeriodId){
                        _logger.LogInformation("Earlier EM evaluation exists, setting status to PENDING, EvalId: {EvalId}", id);
                        evalRecord.StatusCode = "PENDING";
                        _dbContext.Evaluations.Update(evalRecord);
                        _dbContext.SaveChanges();
                        return;
                    }


                    EmissionEvaluation emissionEvalRecord = _dbContext.EmissionEvaluations.Find(monitorPlanId, rptPeriodId); //TODO LOOK UP COMPOSITE PRIMARY KEY
                    emissionEvalRecord.EvalStatus = "WIP";
                    _dbContext.EmissionEvaluations.Update(emissionEvalRecord);
                    _dbContext.SaveChanges();

                    bool evalResult = checkEngine.RunChecks_EmReport(monitorPlanId, rptPeriodId, eCheckEngineRunMode.Normal, es.SetId);
                    _logger.LogInformation("EM checks completed with result: {Result}, EvalId: {EvalId}", 
                        evalResult, id);

                    if (!evalResult) {
                        throw new CheckEngineException("EM Report Check Run Failed.");
                    }

                    _dbContext.Entry<EmissionEvaluation>(emissionEvalRecord).Reload();
                    EvalStatusCode emissionEvalStatus = getStatusCodeByCheckId(emissionEvalRecord.CheckSessionId, evalResult);
                    evaluationStatus = emissionEvalStatus.Code;
                    emissionEvalRecord.EvalStatus = evaluationStatus;
                    _dbContext.EmissionEvaluations.Update(emissionEvalRecord);

                    await _dbContext.ExecuteEmissionRefreshProcedure(monitorPlanId, rp.year, rp.quarter);

                    evaluationStages.Add(new EvaluationStageDto
                    {
                       action = "EM_EVAL_COMPLETED",
                       dateTime = DateTime.UtcNow.ToString("o")
                    });

                    break;
                default:
                    throw new Exception("A Process Code of [MP, QA, EM] is required and was not provided");
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

            _logger.LogInformation("Evaluation {EvalId} completed successfully with status {Status}", 
                id, evaluationStatus);

            // Queue next evaluation(s) based on the process code.
            switch (processCode)
            {
                case "MP":
                    _logger.LogInformation("Checking for QA evaluations for set {SetId}, EvalId: {EvalId}", 
                        es.SetId, id);
                    List<Evaluation> qaEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT *
                        FROM camdecmpsaux.evaluation_queue
                        WHERE process_cd = 'QA' AND evaluation_set_id = {0}
                    ", es.SetId).ToList();
                    if(qaEvals.Count > 0){
                        _logger.LogInformation("Found {Count} QA evaluations to queue for EvalId: {EvalId}", 
                            qaEvals.Count, id);
                        foreach(Evaluation e in qaEvals){
                            e.StatusCode = "QUEUED";
                            _dbContext.Evaluations.Update(e);
                        }
                    } else {
                        _logger.LogInformation("Checking for EM evaluations for monitor plan {MonPlanId}, EvalId: {EvalId}", 
                            es.MonPlanId, id);
                        List<Evaluation> emEvals = _dbContext.Evaluations.FromSqlRaw(@"
                            SELECT eq.*
                            FROM camdecmpsaux.evaluation_queue eq
                            JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                            WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                            ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                        if(emEvals.Count >= 1){
                            _logger.LogInformation("Queueing first EM evaluation, EvalId: {EvalId}", emEvals[0].EvaluationId);
                            emEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
                            _dbContext.Evaluations.Update(emEvals[0]);
                        }
                    }

                    break;
                case "QA":
                    // Check that all QA evaluations have been completed before proceeding to EM evaluations.
                    bool qaEvalsRemain = _dbContext.Evaluations
                      .Where(eq => eq.StatusCode != "COMPLETE" && eq.ProcessCode == "QA" && eq.EvaluationSetId == es.SetId)
                      .Any();

                    if (qaEvalsRemain) break;

                    _logger.LogInformation("Checking for EM evaluations after QA for monitor plan {MonPlanId}", 
                        es.MonPlanId);
                    List<Evaluation> qaEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(qaEmEvals.Count >= 1){
                        _logger.LogInformation("Queueing first EM evaluation after QA, EvalId: {EvalId}", qaEmEvals[0].EvaluationId);
                        qaEmEvals[0].StatusCode = "QUEUED";
                        _dbContext.Evaluations.Update(qaEmEvals[0]);
                    }
                    
                    break;
                case "EM":
                    _logger.LogInformation("Checking for remaining EM evaluations");
                    List<Evaluation> remainingEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                        SELECT eq.*
                        FROM camdecmpsaux.evaluation_queue eq
                        JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                        WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                        ORDER BY eq.rpt_period_id
                        ", es.MonPlanId).ToList();

                    if(remainingEmEvals.Count >= 1){
                        _logger.LogInformation("Queueing next EM evaluation, EvalId: {EvalId}", remainingEmEvals[0].EvaluationId);
                        remainingEmEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
                        _dbContext.Evaluations.Update(remainingEmEvals[0]);
                    }

                    break;
                default:
                    throw new Exception("A Process Code of [MP, QA, EM] is required and was not provided");
            }

            _dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError("Evaluation {EvalId} failed with error: {ErrorMessage}", 
                id, ex.Message);
            _logger.LogError("Full exception details for EvalId: {EvalId} - {Exception}", id, ex.ToString());

            context.MergedJobDataMap.Add("EvaluationResult", "FAILED");
            context.MergedJobDataMap.Add("EvaluationStatus", "FATAL");

            string errorDetails = ex switch
            {
              CheckEngineException cee => $"{cee.Message}\n{checkEngine.CheckEngineErrors}",
              _ => JsonConvert.SerializeObject(ex)
            };
            errorDetails = $"Primary error:\n{errorDetails}";

            var pendingSiblingEvalRecords = _dbContext.Evaluations
              .Where(e => e.EvaluationSetId == evalRecord.EvaluationSetId && e.StatusCode == "PENDING")
              .ToList();

            // Reset the current evaluation and pending sibling evaluations to EVAL status.
            foreach (var record in pendingSiblingEvalRecords.Prepend(evalRecord))
            {
              try
              {
                ResetToNeedsEvaluation(record, es);
              }
              catch (Exception resetEx)
              {
                _logger.LogError("Error resetting evaluation status for EvalId: {EvalId} - {Exception}", record.EvaluationId, resetEx.ToString());
                errorDetails +=
                  $"\n\nError resetting evaluation status for EvalId {record.EvaluationId}:\n{resetEx.Message}\n{resetEx.StackTrace}";
              }
            }

            var noteTime = Utils.getCurrentEasternTime();

            // Update the current evaluation to ERROR status.
            evalRecord.Details = errorDetails;
            evalRecord.StatusCode = "ERROR";
            evalRecord.Note = ex.Message;
            evalRecord.NoteTime = noteTime;
            _dbContext.Evaluations.Update(evalRecord);

            // Update other evaluations in the set to ERROR status.
            foreach (var siblingEval in pendingSiblingEvalRecords)
            {
              siblingEval.StatusCode = "ERROR";
              siblingEval.Note = $"{evalRecord.ProcessCode} evaluation {evalRecord.EvaluationId} failed with error: {ex.Message}";
              siblingEval.NoteTime = noteTime;
              _dbContext.Evaluations.Update(siblingEval);
            }

            _dbContext.SaveChanges();

            // Send the error email
            _ = SendEvaluationErrorEmail(ex.Message, errorDetails, es.SetId, evalRecord.EvaluationId, evaluationStages);
        }
    }

    private void ResetToNeedsEvaluation(Evaluation evalQueueRecord, EvaluationSet evalSetRecord)
    {
      var evalId = evalQueueRecord.EvaluationId;
      switch(evalQueueRecord.ProcessCode){ //Reset status codes to EVAL in case of an evaluation error
          case "MP":
              _logger.LogInformation("Resetting MP evaluation status to EVAL for EvalId: {EvalId}", evalId);
              MonitorPlan mp = _dbContext.MonitorPlans.Find(evalSetRecord.MonPlanId);
              mp.EvalStatus = "EVAL";
              _dbContext.MonitorPlans.Update(mp);
              break;
          case "QA":
              if(!string.IsNullOrWhiteSpace(evalQueueRecord.TestSumId))
              {
                  string testId = evalQueueRecord.TestSumId;
                  _logger.LogInformation("Resetting test summary {TestId} status to EVAL for EvalId: {EvalId}",
                      testId, evalId);
                  TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
                  testSummaryRecord.EvalStatus = "EVAL";
                  _dbContext.TestSummaries.Update(testSummaryRecord);
              }
              else if(!string.IsNullOrWhiteSpace(evalQueueRecord.QaCertEventId))
              {
                  string certId = evalQueueRecord.QaCertEventId;
                  _logger.LogInformation("Resetting QA certification {CertId} status to EVAL for EvalId: {EvalId}",
                      certId, evalId);
                  CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
                  certIdRecord.EvalStatus = "EVAL";
                  _dbContext.CertEvents.Update(certIdRecord);
              }
              else
              {
                  string extensionExemptionId = evalQueueRecord.TeeId;
                  _logger.LogInformation("Resetting extension exemption {ExemptionId} status to EVAL for EvalId: {EvalId}",
                      extensionExemptionId, evalId);
                  TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
                  extensionExemptionRecord.EvalStatus = "EVAL";
                  _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
              }
              break;
          case "EM":
              int rptPeriodId = evalQueueRecord.RptPeriod.Value;
              _logger.LogInformation("Resetting EM evaluation status to EVAL for period {PeriodId}, EvalId: {EvalId}",
                                      rptPeriodId, evalId);
              ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);
              EmissionEvaluation emissionEvalRecord = _dbContext.EmissionEvaluations.Find(evalSetRecord.MonPlanId, rptPeriodId);
              emissionEvalRecord.EvalStatus = "EVAL";
              _dbContext.EmissionEvaluations.Update(emissionEvalRecord);
              break;
      }

      _dbContext.SaveChanges();
    }

    private async Task SendEvaluationErrorEmail(string errorMessage, string errorDetails, string evaluationSetId, long evaluationId, List<EvaluationStageDto> evaluationStages)
    {
        try
        {
            var client = new HttpClient();

            // Trim stack trace to reasonable length (max 8000 chars)
            string stackTrace = errorDetails;
            if (!string.IsNullOrEmpty(stackTrace) && stackTrace.Length > 8000)
            {
                stackTrace = stackTrace.Substring(0, 8000) + "\n... [Stack trace truncated]";
            }

            // Populate request payload
            var payload = new
            {
                evaluationSetId = evaluationSetId,
                evaluationId = evaluationId,
                rootError = errorMessage,
                errorStack = stackTrace,
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
                _logger.LogInformation("Evaluation error email sent successfully for EvaluationSet ID: {EvaluationSetId}, EvalId: {EvalId}", evaluationSetId, evaluationId);
            }
            else
            {
                _logger.LogError("Failed to send evaluation error email for EvalId: {EvalId}. Status Code: {StatusCode}, Reason: {ReasonPhrase}", 
                    evaluationId, response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error sending evaluation error email for EvalId: {EvalId}: {ErrorMessage}", evaluationId, e.Message);
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
