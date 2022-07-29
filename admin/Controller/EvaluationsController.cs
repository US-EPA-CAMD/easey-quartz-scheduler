using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler
{
  [ApiController]
  [Produces("application/json")]
  [Route("quartz-mgmt/triggers/evaluations")]
  public class EvaluationsController : ControllerBase
  {
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }


    public EvaluationsController(
      NpgSqlContext dbContext,
      IConfiguration configuration
    )
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    private async Task<ActionResult> TriggerCheckEngineEvaluation(
      string processCode,
      string monitorPlanId,
      string userId,
      string userEmail
    )
    {
      Services services = (Services)Request.HttpContext.Items[typeof(Services)];

      Guid id = Guid.NewGuid();
      DateTime submittedOn = DateTime.Now;

      MonitorPlan mp = _dbContext.MonitorPlans.Find(monitorPlanId);
      Facility fac = _dbContext.Facilities.Find(mp.FacilityId);
      List<MonitorLocation> locs = _dbContext.MonitorLocations.FromSqlRaw(@"
        SELECT ml.mon_loc_id, u.unit_id, u.unitid, sp.stack_pipe_id, sp.stack_name
        FROM camdecmpswks.monitor_location ml
          JOIN camdecmpswks.monitor_plan_location USING(mon_loc_id)
          LEFT JOIN camd.unit u ON ml.unit_id = u.unit_id
          LEFT JOIN camdecmpswks.stack_pipe sp ON ml.stack_pipe_id = sp.stack_pipe_id
        WHERE mon_plan_id = {0}
        ORDER BY unitId, stack_name",
        monitorPlanId
      ).ToList();

      string monPlanConfig = string.Join(", ", locs.Select(x =>
        string.IsNullOrWhiteSpace(x.UnitName) ? x.StackName : x.UnitName
      ));

      await CheckEngineEvaluation.StartNow(
        services.Scheduler,
        id,
        processCode,
        fac.OrisCode,
        fac.Name,
        monitorPlanId,
        monPlanConfig,
        userId,
        userEmail,
        submittedOn
      );

      mp.EvalStatus = "INQ"; // set eval status to In Queue
      _dbContext.MonitorPlans.Update(mp);
      _dbContext.SaveChanges();

      return CreatedAtAction("EvaluationResponse)", new
      {
        id = id,
        processCode = processCode,
        facilityId = fac.OrisCode,
        facilityName = fac.Name,
        monitorPlanId = monitorPlanId,
        configuration = monPlanConfig,
        userId = userId,
        userEmail = userEmail,
        submittedOn = submittedOn
      });
    }

    [HttpPost("monitor-plans")]
    public async Task<ActionResult> TriggerMPEvaluation([FromBody] EvaluationRequest request)
    {

      /*
      string errorMessage = await Utils.validateRequestCredentialsUserToken(Request, Configuration);
      if(errorMessage != "")
      {
        return BadRequest(errorMessage);
      }
      */

      return await TriggerCheckEngineEvaluation(
        "MP",
        request.MonitorPlanId,
        request.UserId,
        request.UserEmail
      );
    }
  }
}
