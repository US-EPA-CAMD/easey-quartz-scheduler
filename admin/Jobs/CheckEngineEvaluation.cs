using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class CheckEngineEvaluation : IJob
  {
    private readonly ILogger _logger;
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }

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
      IConfiguration configuration,
      ILogger<CheckEngineEvaluation> logger
    )
    {
      _logger = logger;
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public Task Execute(IJobExecutionContext context)
    {
      try
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

        LogHelper.info(
          _logger, $"Executing {key.Group}.{key.Name} with data map...",
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

        /////////////////////////////////////////////////////////////////////////////////////////////
        // PLUGIN IN CHECK ENGINE HERE

        // Remove once actual Check Engine has bee plugged in
        System.Threading.Thread.Sleep(5000);

        // TODO: instantiate Check Engine and execute the proper process based on the process code
        LogHelper.info(_logger, "Executing Checks...");

        // Remove once actual Check Engine has bee plugged in
        System.Threading.Thread.Sleep(30000);
        /////////////////////////////////////////////////////////////////////////////////////////////

        // Need to retrieve again to get session id that was set by evaluation job
        mp = _dbContext.MonitorPlans.Find(monitorPlanId);
        CheckSession chkSession = _dbContext.CheckSessions.Find(mp.CheckSessionId);

        switch(chkSession.SeverityCode)
        {
          case "NONE":
            mp.EvalStatus = "PASS";
            break;
          case "ADMNOVR":
            mp.EvalStatus = ""; // TODO: NEED SEVERITY CODE TO EVAL STATUS MAPPING
            break;
          case "INFORM":
            mp.EvalStatus = "INFO";
            break;
          case "NONCRIT":
            mp.EvalStatus = ""; // TODO: NEED SEVERITY CODE TO EVAL STATUS MAPPING
            break;
          case "CRIT1":
          case "CRIT2":
          case "CRIT3":
          case "FATAL":          
            mp.EvalStatus = "ERR";
            break;
        }

        context.MergedJobDataMap.Add("EvaluationStauts", mp.EvalStatus);

        _dbContext.MonitorPlans.Update(mp);
        _dbContext.SaveChanges();

        LogHelper.info(_logger, $"{key.Group}.{key.Name} completed successfully");
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        LogHelper.error(_logger, ex.ToString());
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
      DateTime submittedOn
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
        .StartNow()
        .Build();

      await scheduler.ScheduleJob(job, trigger);
    }
  }
}