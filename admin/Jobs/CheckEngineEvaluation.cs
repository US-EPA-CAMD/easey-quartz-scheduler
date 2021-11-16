using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Quartz;
using Epa.Camd.Easey.JobScheduler.Models;
using Epa.Camd.Easey.JobScheduler.Logging;

namespace Epa.Camd.Easey.JobScheduler.Jobs
{
  public class CheckEngineEvaluation : IJob
  {
    private readonly ILogger _logger;

    private NpgSqlContext _dbContext = null;

    public CheckEngineEvaluation(NpgSqlContext dbContext, ILogger<CheckEngineEvaluation> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
      try
      {
        JobKey key = context.JobDetail.Key;
        JobDataMap dataMap = context.MergedJobDataMap;

        string id = dataMap.GetString("Id");
        string userId = dataMap.GetString("UserId");
        string monPlanId = dataMap.GetString("MonitorPlanId");
        string processCode = dataMap.GetString("ProcessCode");
        string submittedOn = dataMap.GetString("SubmittedOn");

        LogHelper.info(
          _logger, $"Executing {key.Group}.{key.Name} with data map...",
          new LogVariable("Id", id),
          new LogVariable("Process Code", processCode),
          new LogVariable("Monior Plan Id", monPlanId),
          new LogVariable("User Id", userId),
          new LogVariable("Submitted On", submittedOn)
        );

        MonitorPlan mp = _dbContext.MonitorPlans.Find(monPlanId);
        Facility fac = _dbContext.Facilities.Find(mp.FacilityId);

        List<MonitorPlanLocation> mpl = _dbContext.MonitorPlanLocations.FromSqlRaw(
          @"SELECT * FROM camdecmpswks.monitor_plan_location WHERE mon_plan_id = {0}",
          monPlanId
        ).ToList();

        string[] locationIds = mpl.Select(x => x.LocationId).ToArray();
        
        List<MonitorLocation> mlocs = _dbContext.MonitorLocations.FromSqlRaw(@"
          SELECT ml.mon_loc_id, u.unit_id, u.unitid, sp.stack_pipe_id, sp.stack_name
          FROM camdecmpswks.monitor_location ml
          LEFT JOIN camd.unit u ON ml.unit_id = u.unit_id
          LEFT JOIN camdecmpswks.stack_pipe sp ON ml.stack_pipe_id = sp.stack_pipe_id
          WHERE mon_loc_id IN ({0})
          ORDER BY unitId, stack_name",
          locationIds
        ).ToList();

        string config = string.Join(", ", mlocs.Select(x =>
          string.IsNullOrWhiteSpace(x.UnitName) ? x.StackName : x.UnitName
        ));

        //System.Threading.Thread.Sleep(30000);
        //item.StatusCode = "Complete";
        //_dbContext.Submissions.Update(item);
        //_dbContext.SaveChanges();

        //await Console.Out.WriteLineAsync($"{key.Group} process ended for {key.Name}...");

        LogHelper.info(_logger, "Executed CheckEngineEvaluation job successfully");
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        LogHelper.error(_logger, ex.ToString());
        return Task.FromException(ex);
      }
    }

    public static async Task StartNow(
      IScheduler scheduler,
      Guid id,
      string processCode,
      string monitorPlanId,
      string userId,
      DateTime submittedOn
    ) {
      IJobDetail job = JobBuilder.Create<CheckEngineEvaluation>()
        .WithIdentity(
          Constants.JobDetails.CHECK_ENGINE_EVALUATION_KEY,
          Constants.JobDetails.CHECK_ENGINE_EVALUATION_GROUP
        )
        .UsingJobData("ProcessCode", processCode)
        .Build();

      ITrigger trigger = TriggerBuilder.Create()
        .UsingJobData("Id", id.ToString())
        .UsingJobData("MonitorPlanId", monitorPlanId)
        .UsingJobData("UserId", userId)
        .UsingJobData("SubmittedOn", submittedOn.ToString())
        .StartNow()
        .Build();

      await scheduler.ScheduleJob(job, trigger);
    }

    public static JobKey WithJobKey()
    {
       return new JobKey(
           Constants.JobDetails.CHECK_ENGINE_EVALUATION_KEY,
           Constants.JobDetails.CHECK_ENGINE_EVALUATION_GROUP
       );
    }
    public static TriggerKey WithTriggerKey()
    {
        return new TriggerKey(
           Constants.JobDetails.CHECK_ENGINE_EVALUATION_KEY,
           Constants.JobDetails.CHECK_ENGINE_EVALUATION_GROUP
        );
    }
  }
}