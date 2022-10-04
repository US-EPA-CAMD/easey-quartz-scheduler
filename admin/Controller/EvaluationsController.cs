using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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
      string userEmail,
      List<string> qaCertEventId = null,
      List<string> testExtensionExemptionId = null,
      List<string> testSumId = null
    )
    {

      // Optional input for QA lists, default to empty list
      qaCertEventId = qaCertEventId ?? new List<string>();
      testExtensionExemptionId = testExtensionExemptionId ?? new List<string>();
      testSumId = testSumId ?? new List<string>();

      Services services = (Services)Request.HttpContext.Items[typeof(Services)];

      Guid id = Guid.NewGuid();
      DateTime submittedOn = Utils.getCurrentEasternTime();

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

      // Should we regenerate all of the ids?
      if(processCode == "QA" && qaCertEventId.Count == 0 && testExtensionExemptionId.Count == 0 && testSumId.Count == 0){
        foreach(MonitorLocation loc in locs){
          List<List<object>> qaCertRows = await _dbContext.ExecuteSqlQuery("SELECT qa_cert_event_id FROM camdecmpswks.qa_cert_event WHERE mon_loc_id = '" + loc.Id + "'", 1);
          for(int i = 0; i < qaCertRows.Count; i++){
            qaCertEventId.Add((string) qaCertRows[i][0]);
          }
          List<List<object>> testExtensionExemptionRows = await _dbContext.ExecuteSqlQuery("SELECT test_extension_exemption_id FROM camdecmpswks.test_extension_exemption WHERE mon_loc_id = '" + loc.Id + "'", 1);
          for(int i = 0; i < testExtensionExemptionRows.Count; i++){
            testExtensionExemptionId.Add((string) testExtensionExemptionRows[i][0]);
          }
          List<List<object>> testSumRows = await _dbContext.ExecuteSqlQuery("SELECT test_sum_id FROM camdecmpswks.test_summary WHERE mon_loc_id = '" + loc.Id + "'", 1);
          for(int i = 0; i < testSumRows.Count; i++){
            testSumId.Add((string) testSumRows[i][0]);
          }
        }
      }

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
        submittedOn,
        JsonConvert.SerializeObject(qaCertEventId),
        JsonConvert.SerializeObject(testExtensionExemptionId),
        JsonConvert.SerializeObject(testSumId)
      );


      switch(processCode){
        case "MP": 
            mp.EvalStatus = "INQ"; // set eval status to In Queue
            _dbContext.MonitorPlans.Update(mp);
          break;
        case "QA":
            foreach (string certId in qaCertEventId)
            {
                CertEvent certIdRecord = _dbContext.CertEvents.Find(certId);
                certIdRecord.EvalStatus = "INQ";
                _dbContext.CertEvents.Update(certIdRecord);
            }

            foreach (string extensionExemptionId in testExtensionExemptionId)
            {
                TestExtensionExemption extensionExemptionRecord = _dbContext.TestExtensionExemptions.Find(extensionExemptionId);
                extensionExemptionRecord.EvalStatus = "INQ";
                _dbContext.TestExtensionExemptions.Update(extensionExemptionRecord);
            }

            foreach (string testId in testSumId)
            {
                TestSummary testSummaryRecord = _dbContext.TestSummaries.Find(testId);
                testSummaryRecord.EvalStatus = "INQ";
                _dbContext.TestSummaries.Update(testSummaryRecord);
            }
          break;

        case "EM": 
          break;
      }

      _dbContext.SaveChanges(); 

      return CreatedAtAction("EvaluationResponse", new
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

    [HttpPost("qa-certifications")]
    public async Task<ActionResult> TriggerQAEvaluation([FromBody] QaEvaluationRequest request)
    {

      string errorMsg;
      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"])){
        errorMsg = Utils.validateRequestCredentialsGatewayToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }

      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_USER_TOKEN"])){
        errorMsg = await Utils.validateRequestCredentialsUserToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }

      return await TriggerCheckEngineEvaluation(
        "QA",
        request.MonitorPlanId,
        request.UserId,
        request.UserEmail,
        request.qaCertEventId,
        request.testExtensionExemptionId,
        request.testSumId
      );
    }

    [HttpPost("monitor-plans")]
    public async Task<ActionResult> TriggerMPEvaluation([FromBody] EvaluationRequest request)
    {

      
      string errorMsg;
      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"])){
        errorMsg = Utils.validateRequestCredentialsGatewayToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }
      

      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_USER_TOKEN"])){
        errorMsg = await Utils.validateRequestCredentialsUserToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }
    
      return await TriggerCheckEngineEvaluation(
        "MP",
        request.MonitorPlanId,
        request.UserId,
        request.UserEmail
      );
    }
  }
}
