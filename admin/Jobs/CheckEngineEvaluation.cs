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

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class CheckEngineEvaluation : IJob
  {
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }
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
      IConfiguration configuration
    )
    {
      _dbContext = dbContext;
      Configuration = configuration;
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
      JobDataMap dataMap = context.MergedJobDataMap;
      JobKey key = context.JobDetail.Key;

      string id = dataMap.GetString("Id");

      Evaluation evalRecord = _dbContext.Evaluations.Find(Int64.Parse(id));

      evalRecord.StatusCode = "WIP";
      evalRecord.StartedTime = DateTime.Now;
      _dbContext.Evaluations.Update(evalRecord);
      _dbContext.SaveChanges();

      string processCode = dataMap.GetString("ProcessCode");
      int facilityId = dataMap.GetIntValue("FacilityId");
      string facilityName = dataMap.GetString("FacilityName");
      string monitorPlanId = dataMap.GetString("MonitorPlanId");
      string monPlanConfig = dataMap.GetString("Configuration");
      string userId = dataMap.GetString("UserId");
      string userEmail = dataMap.GetString("UserEmail");
      string queuedTime = dataMap.GetString("QueuedTime");

      try
      {
        string connectionString = ConnectionStringManager.getConnectionString(Configuration);

        LogHelper.info(
          $"Executing {key.Group}.{key.Name} with data map...",
          new LogVariable("Id", id),
          new LogVariable("Process Code", processCode),
          new LogVariable("Facility Id", facilityId),
          new LogVariable("Facility Name", facilityName),
          new LogVariable("Monior Plan Id", monitorPlanId),
          new LogVariable("Configuration", monPlanConfig),
          new LogVariable("User Id", userId),
          new LogVariable("User Email", userEmail),
          new LogVariable("Queued Time", queuedTime)
        );

        string dllPath = Configuration["EASEY_QUARTZ_SCHEDULER_CHECK_ENGINE_DLL_PATH"];
        cCheckEngine checkEngine = new cCheckEngine(userId, connectionString, dllPath, "dumpfilePath", 20);

        MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);

        string evaluationStatus = "";

        EvaluationSet es = _dbContext.EvaluationSet.Find(dataMap.GetString("SetId"));

        switch (processCode)
        {
          case "MP":
            LogHelper.info("Running RunChecks_MpReport...");

            mp.EvalStatus = "WIP";
            _dbContext.MonitorPlans.Update(mp);
            _dbContext.SaveChanges();

            bool mpResult = checkEngine.RunChecks_MpReport(monitorPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, es.SetId);

            _dbContext.Entry<MonitorPlan>(mp).Reload();
            EvalStatusCode evalStatus = getStatusCodeByCheckId(mp.CheckSessionId, mpResult);
            mp.EvalStatus = evalStatus.Code;
            evaluationStatus = evalStatus.Code;
            _dbContext.MonitorPlans.Update(mp);
            context.MergedJobDataMap.Add("EvaluationStatus", evalStatus.Description);

            // Logic to flip state and unlock qa or emissions files after run of MP
            List<Evaluation> qaEvals = _dbContext.Evaluations.FromSqlRaw(@"
              SELECT *
              FROM camdecmpsaux.evaluation_queue
              WHERE process_cd = 'QA' AND evaluation_set_id = {0}
            ", es.SetId).ToList();
            if(qaEvals.Count > 0){
              foreach(Evaluation e in qaEvals){
                e.StatusCode = "QUEUED";
                _dbContext.Evaluations.Update(e);
              }
            }else{
              List<Evaluation> emEvals = _dbContext.Evaluations.FromSqlRaw(@"
                SELECT eq.*
                FROM camdecmpsaux.evaluation_queue eq
                JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                ORDER BY eq.rpt_period_id
                ", es.MonPlanId).ToList();

              if(emEvals.Count >= 1){
                emEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
               _dbContext.Evaluations.Update(emEvals[0]);
              }
            }
            // --------

            LogHelper.info($"RunChecks_MpReport returned a result of {mpResult}!");
            break;
          case "QA":
            LogHelper.info("Running QA import checks...");
            if(!string.IsNullOrWhiteSpace(dataMap.GetString("testSumId"))){
              string testId = dataMap.GetString("testSumId");
              TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
              testSummaryRecord.EvalStatus = "WIP";
              _dbContext.TestSummaries.Update(testSummaryRecord);

              bool listResult = checkEngine.RunChecks_QaReport_Test(testId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);

              _dbContext.Entry<TestSummary>(testSummaryRecord).Reload();
              EvalStatusCode testSumEvalStatus = getStatusCodeByCheckId(testSummaryRecord.CheckSessionId, listResult);
              evaluationStatus = testSumEvalStatus.Code;
              testSummaryRecord.EvalStatus = testSumEvalStatus.Code;
              _dbContext.TestSummaries.Update(testSummaryRecord);
            }
            else if(!string.IsNullOrWhiteSpace(dataMap.GetString("qaCertId"))){
              string certId = dataMap.GetString("qaCertId");
              CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
              certIdRecord.EvalStatus = "WIP";
              _dbContext.CertEvents.Update(certIdRecord);
              _dbContext.SaveChanges();

              bool listResult = checkEngine.RunChecks_QaReport_Qce(certId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);

              _dbContext.Entry<CertEvent>(certIdRecord).Reload();
              EvalStatusCode certEvalStatus = getStatusCodeByCheckId(certIdRecord.CheckSessionId, listResult);
              evaluationStatus = certEvalStatus.Code;
              certIdRecord.EvalStatus = certEvalStatus.Code;
              _dbContext.CertEvents.Update(certIdRecord);
            }
            else{
              string extensionExemptionId = dataMap.GetString("testExtensionExemption");
              TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
              extensionExemptionRecord.EvalStatus = "WIP";
              _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
              _dbContext.SaveChanges();

              bool listResult = checkEngine.RunChecks_QaReport_Tee(extensionExemptionId, monitorPlanId, eCheckEngineRunMode.Normal, es.SetId);

              _dbContext.Entry<TestExtensionExemption>(extensionExemptionRecord).Reload();
              EvalStatusCode teeEvalStatus = getStatusCodeByCheckId(extensionExemptionRecord.CheckSessionId, listResult);
              evaluationStatus = teeEvalStatus.Code;
              extensionExemptionRecord.EvalStatus = teeEvalStatus.Code;
              _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
            }

            // Flip Emissions Records After QA Is Run
            List<Evaluation> qaEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                SELECT eq.*
                FROM camdecmpsaux.evaluation_queue eq
                JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                ORDER BY eq.rpt_period_id
                ", es.MonPlanId).ToList();

            if(qaEmEvals.Count >= 1){
              qaEmEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
              _dbContext.Evaluations.Update(qaEmEvals[0]);
            }
            
    
            LogHelper.info($"QA import checks finished");

            break;
          case "EM":
            int rptPeriodId = Int32.Parse(dataMap.GetString("rptPeriodId"));
            ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);

            //During each emission evaluation fetch the latest queued or pending emissions records
             List<Evaluation> otherEmEvals = _dbContext.Evaluations.FromSqlRaw(@" 
                SELECT eq.*
                FROM camdecmpsaux.evaluation_queue eq
                JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                WHERE eq.process_cd = 'EM' AND eq.status_cd IN ('PENDING', 'QUEUED', 'WIP') AND es.mon_plan_id = {0}
                ORDER BY eq.rpt_period_id
                ", es.MonPlanId).ToList();

            if(otherEmEvals[0].RptPeriod != rptPeriodId){
              //There is an earlier record in pending, queued, or wip status for emissions
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
            _dbContext.Entry<EmissionEvaluation>(emissionEvalRecord).Reload();
            EvalStatusCode emissionEvalStatus = getStatusCodeByCheckId(emissionEvalRecord.CheckSessionId, evalResult);
            evaluationStatus = emissionEvalStatus.Code;
            emissionEvalRecord.EvalStatus = evaluationStatus;
            _dbContext.EmissionEvaluations.Update(emissionEvalRecord);

            _dbContext.ExecuteEmissionRefreshProcedure(monitorPlanId, rp.year, rp.quarter);


            List<Evaluation> remainingEmEvals = _dbContext.Evaluations.FromSqlRaw(@"
                SELECT eq.*
                FROM camdecmpsaux.evaluation_queue eq
                JOIN camdecmpsaux.evaluation_set es USING(evaluation_set_id)
                WHERE eq.process_cd = 'EM' AND eq.status_cd = 'PENDING' AND es.mon_plan_id = {0}
                ORDER BY eq.rpt_period_id
                ", es.MonPlanId).ToList();

            if(remainingEmEvals.Count >= 1){
              remainingEmEvals[0].StatusCode = "QUEUED"; //Only take the first EM record with the earliest rpt_period_id, let the EM portion of this job handle scheduling the others
              _dbContext.Evaluations.Update(remainingEmEvals[0]);
            }


            break;
          default:
            throw new Exception("A Process Code of [MP, QA-QCE, QA-TEE, EM] is required and was not provided");
        }

        context.MergedJobDataMap.Add("EvaluationResult", "COMPLETED");

        // Update our queued record
        evalRecord.StatusCode = "COMPLETE";
        evalRecord.CompletedTime = DateTime.Now;
        evalRecord.EvalStatusCode = evaluationStatus;
        _dbContext.Evaluations.Update(evalRecord);
        _dbContext.SaveChanges();

        LogHelper.info($"{key.Group}.{key.Name} COMPLETED successfully!");
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        evalRecord.Details = JsonConvert.SerializeObject(ex);

        evalRecord.StatusCode = "ERROR";
        evalRecord.Note = ex.Message;
        evalRecord.NoteTime = DateTime.Now;
        _dbContext.Evaluations.Update(evalRecord);
        _dbContext.SaveChanges();

        context.MergedJobDataMap.Add("EvaluationResult", "FAILED");
        context.MergedJobDataMap.Add("EvaluationStatus", "FATAL");
        LogHelper.error(ex.ToString());


        switch(processCode){ //Reset status codes to EVAL in case of an evaluation error
          case "MP":
            MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);
            mp.EvalStatus = "EVAL";
            _dbContext.MonitorPlans.Update(mp);
            break;
          case "QA":
            if(!string.IsNullOrWhiteSpace(dataMap.GetString("testSumId"))){
              string testId = dataMap.GetString("testSumId");
              TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
              testSummaryRecord.EvalStatus = "EVAL";
              _dbContext.TestSummaries.Update(testSummaryRecord);
            }
            else if(!string.IsNullOrWhiteSpace(dataMap.GetString("qaCertId"))){
              string certId = dataMap.GetString("qaCertId");
              CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
              certIdRecord.EvalStatus = "EVAL";
              _dbContext.CertEvents.Update(certIdRecord);
            }
            else{
              string extensionExemptionId = dataMap.GetString("testExtensionExemption");
              TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
              extensionExemptionRecord.EvalStatus = "EVAL";
              _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
            }
            break;
          case "EM":
            int rptPeriodId = Int32.Parse(dataMap.GetString("rptPeriodId"));
            ReportingPeriod rp = _dbContext.ReportingPeriods.Find(rptPeriodId);
            EmissionEvaluation emissionEvalRecord = _dbContext.EmissionEvaluations.Find(monitorPlanId, rptPeriodId); //TODO LOOK UP COMPOSITE PRIMARY KEY
            emissionEvalRecord.EvalStatus = "EVAL";
            _dbContext.EmissionEvaluations.Update(emissionEvalRecord);
            break;
        }
        _dbContext.SaveChanges();

        return Task.FromException(ex);
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
