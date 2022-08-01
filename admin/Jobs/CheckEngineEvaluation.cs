using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;


using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;

using DatabaseAccess;
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;

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
      public static readonly string JobName = "{0} Evaluation";
      public static readonly string JobDescription = "Evaluates a {0} data set for accuracy as specified by the EPA Part 75 reporting instructions.";
      public static readonly string TriggerName = "{0} Evaluation ({1} {2})";
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

      int maxThreads = Int32.Parse(configuration["EASEY_QUARTZ_SCHEDULER_MAX_CHECK_THREADS"]);
      semaphore = new SemaphoreSlim(maxThreads, maxThreads);
    }

    public Task Execute(IJobExecutionContext context)
    {
      JobKey key = context.JobDetail.Key;

      try
      {
        JobDataMap dataMap = context.MergedJobDataMap;

        bool result = false;
        string id = dataMap.GetString("Id");
        string processCode = dataMap.GetString("ProcessCode");
        int facilityId = dataMap.GetIntValue("FacilityId");
        string facilityName = dataMap.GetString("FacilityName");
        string monitorPlanId = dataMap.GetString("MonitorPlanId");
        string monPlanConfig = dataMap.GetString("Configuration");
        string userId = dataMap.GetString("UserId");
        string userEmail = dataMap.GetString("UserEmail");
        string submittedOn = dataMap.GetString("SubmittedOn");
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
          new LogVariable("Submitted On", submittedOn)
        );

        MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);
        mp.EvalStatus = "WIP";
        _dbContext.MonitorPlans.Update(mp);
        _dbContext.SaveChanges();

        string dllPath;
        cCheckEngine checkEngine;

        switch (processCode)
        {
          case "MP":
            dllPath = Configuration["EASEY_QUARTZ_SCHEDULER_CHECK_ENGINE_DLL_PATH"];
            checkEngine = new cCheckEngine(userId, connectionString, dllPath, "dumpfilePath", 20);

            LogHelper.info("Running RunChecks_MpReport...");
            result = checkEngine.RunChecks_MpReport(monitorPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal);
            LogHelper.info($"RunChecks_MpReport returned a result of {result}!");

            break;
          case "QA":
            dllPath = Configuration["EASEY_QUARTZ_SCHEDULER_CHECK_ENGINE_DLL_PATH"];
            checkEngine = new cCheckEngine(userId, connectionString, dllPath, "dumpfilePath", 20);

            LogHelper.info("Running QA import checks...");

            List<string> qaCertEventId = JsonConvert.DeserializeObject<List<string>>(dataMap.GetString("qaCertId"));
            List<string> testExtensionExemptionId = JsonConvert.DeserializeObject<List<string>>(dataMap.GetString("testExtensionExemption"));
            List<string> testSumId = JsonConvert.DeserializeObject<List<string>>(dataMap.GetString("testSumId"));

            int numberOfRecordsToGen = qaCertEventId.Count + testExtensionExemptionId.Count + testSumId.Count;

            string batchId = null;
            if(numberOfRecordsToGen > 1){ //We need to initialize a batch, there is more than one record coming in
              batchId = Guid.NewGuid().ToString();
            }

            List<Task> threadPool = new List<Task>();

            Console.WriteLine("{0} tasks can enter the semaphore.", semaphore.CurrentCount);

            foreach (string certId in qaCertEventId)
            {
                threadPool.Add(Task.Run(() => {
                  semaphore.Wait();
                  try{
                    //checkEngine.RunChecks_QaReport_Qce(certId, monitorPlanId, eCheckEngineRunMode.Normal, batchId);
                  }
                  finally{
                    semaphore.Release();
                  }
                }));
            }

            foreach (string extensionExemptionId in testExtensionExemptionId)
            {
                threadPool.Add(Task.Run(() => {
                  semaphore.Wait();
                  try{
                    //checkEngine.RunChecks_QaReport_Tee(extensionExemptionId, monitorPlanId, eCheckEngineRunMode.Normal, batchId);
                  }finally{
                    semaphore.Release();
                  }
                  return;
                }));
            }

            foreach (string testId in testSumId)
            {
                threadPool.Add(Task.Run(() => {
                  semaphore.Wait();
                  try{
                    //checkEngine.RunChecks_QaReport_Test(testId, monitorPlanId, eCheckEngineRunMode.Normal, batchId);
                  }finally{
                    semaphore.Release();
                  }
                }));
            }

            LogHelper.info($"Waiting on QA checks");

            Task.WaitAll(threadPool.ToArray());

            LogHelper.info($"QA import checks finished");

            break;
          case "EM":
            LogHelper.info("Running RunChecks_EmReport...");
            // result = this.RunChecks_EmReport();
            break;
          default:
            throw new Exception("A Process Code of [MP, QA-QCE, QA-TEE, EM] is required and was not provided");
        }

        EvalStatusCode evalStatus;
        _dbContext.Entry<MonitorPlan>(mp).Reload();

        if (result)
        {
          CheckSession chkSession = _dbContext.CheckSessions.Find(mp.CheckSessionId);
          SeverityCode severity = _dbContext.SeverityCodes.Find(chkSession.SeverityCode);
          evalStatus = _dbContext.EvalStatusCodes.Find(severity.EvalStatusCode);
        }
        else
        {
          // TODO: MAY NEED A STATUS FOR THIS SITUATION WHERE CHECK ENGINE RETURNS FALSE AND NO SESSION ID TO LOOKUP EVAL STATUS
          evalStatus = _dbContext.EvalStatusCodes.Find("ERR");
        }        

        mp.EvalStatus = evalStatus.Code;
        _dbContext.MonitorPlans.Update(mp);
        _dbContext.SaveChanges();

        context.MergedJobDataMap.Add("EvaluationResult", "COMPLETED");
        context.MergedJobDataMap.Add("EvaluationStatus", evalStatus.Description);

        LogHelper.info($"{key.Group}.{key.Name} COMPLETED successfully!");
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        context.MergedJobDataMap.Add("EvaluationResult", "FAILED");
        context.MergedJobDataMap.Add("EvaluationStatus", "FATAL");
        LogHelper.error(ex.ToString());
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
          GetProcess(processCode)
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
            configuration
          ),
          Identity.Group
        );
    }

    public static async Task StartNow(
      IScheduler scheduler,
      Guid id,
      string processCode,
      int facilityId,
      string facilityName,
      string monitorPlanId,
      string monPlanConfig,
      string userId,
      string userEmail,
      DateTime submittedOn,
      string qaCertEventIdJSON,
      string testExtensionExemptionIdJSON,
      string testSumIdJSON
    ) {
      string processName = GetProcess(processCode);

      IJobDetail job = JobBuilder.Create<CheckEngineEvaluation>()
        .WithIdentity(WithJobKey(processCode))
        .WithDescription(string.Format(Identity.JobDescription, processName))
        .UsingJobData("ProcessCode", processCode)
        .Build();

      ITrigger trigger = TriggerBuilder.Create()
        .WithIdentity(WithTriggerKey(processCode, facilityName, monPlanConfig))
        .WithDescription(string.Format(Identity.TriggerDescription, processName, facilityName, monPlanConfig))
        .UsingJobData("Id", id.ToString())
        .UsingJobData("FacilityId", facilityId)
        .UsingJobData("FacilityName", facilityName)
        .UsingJobData("MonitorPlanId", monitorPlanId)
        .UsingJobData("Configuration", monPlanConfig)
        .UsingJobData("UserId", userId)
        .UsingJobData("UserEmail", userEmail)        
        .UsingJobData("SubmittedOn", submittedOn.ToString())
        .UsingJobData("qaCertId", qaCertEventIdJSON)
        .UsingJobData("testExtensionExemption", testExtensionExemptionIdJSON)
        .UsingJobData("testSumId", testSumIdJSON)        
        .StartNow()
        .Build();

      await scheduler.ScheduleJob(job, trigger);
    }
  }
}