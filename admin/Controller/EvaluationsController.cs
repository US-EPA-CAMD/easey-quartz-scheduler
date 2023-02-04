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

    /*
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

    [HttpPost("emissions")]
    public async Task<ActionResult> TriggerEmissionsEvaluation([FromBody] EmmisionsEvaluationRequest request)
    {

        string errorMsg;
        if (Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"]))
        {
            errorMsg = Utils.validateRequestCredentialsGatewayToken(Request, Configuration);
            if (errorMsg != "")
            {
                return BadRequest(errorMsg);
            }
        }

        if (Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_USER_TOKEN"]))
        {
            errorMsg = await Utils.validateRequestCredentialsUserToken(Request, Configuration);
            if (errorMsg != "")
            {
                return BadRequest(errorMsg);
            }
        }

        return null;

        return await TriggerCheckEngineEvaluation(
          "EM",
          request.MonitorPlanId,
          request.UserId,
          request.UserEmail,
          null,
          null,
          null,
          request.RptPeriodId
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

      MonitorPlan mp = _dbContext.MonitorPlans.Find(request.MonitorPlanId);

      
    
      return await TriggerCheckEngineEvaluation(
        "MP",
        request.MonitorPlanId,
        request.UserId,
        request.UserEmail
      );
    }
    */

    [HttpPost()]
    public async Task<ActionResult> Evaluate([FromBody] BulkEvaluationRequest request)
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

      foreach (EvaluationItem item in request.items)
      {

        try{
            DateTime currentEasternTime = Utils.getCurrentEasternTime();
            Guid set_id = Guid.NewGuid();

            // Create Evaluation Set Record For Each Record -----------------

            EvaluationSet es = new EvaluationSet();
            es.SetId = set_id.ToString();
            es.MonPlanId = item.monPlanId;
            es.UserId = request.UserId;
            es.UserEmail = request.UserEmail;
            es.SubmittedOn = currentEasternTime;

            MonitorLocation loc = _dbContext.MonitorLocations.FromSqlRaw(@"
              SELECT ml.mon_loc_id, u.unit_id, u.unitid, sp.stack_pipe_id, sp.stack_name
              FROM camdecmpswks.monitor_location ml
                JOIN camdecmpswks.monitor_plan_location USING(mon_loc_id)
                LEFT JOIN camd.unit u ON ml.unit_id = u.unit_id
                LEFT JOIN camdecmpswks.stack_pipe sp ON ml.stack_pipe_id = sp.stack_pipe_id
              WHERE mon_plan_id = {0}
              ORDER BY unitId, stack_name",
              item.monPlanId
            ).First();

            es.Config = string.IsNullOrWhiteSpace(loc.UnitName) ? loc.StackName : loc.UnitName;

            MonitorPlan mp = _dbContext.MonitorPlans.Find(item.monPlanId);

            Facility f = _dbContext.Facilities.Find(mp.FacilityId);
            es.FacName = f.Name;
            es.FacId = f.OrisCode;

            _dbContext.EvaluationSet.Add(es);
            _dbContext.SaveChanges();

            //-------------------------------------------------

            if(item.submitMonPlan == true){ //Create monitor plan queue record
              mp.EvalStatus = "INQ";
              _dbContext.MonitorPlans.Update(mp);

              Evaluation mpRecord = new Evaluation();
              mpRecord.EvaluationSetId = set_id.ToString();
              mpRecord.ProcessCode = "MP";
              mpRecord.StatusCode = "QUEUED";
              mpRecord.SubmittedOn = currentEasternTime; 
              _dbContext.Evaluations.Add(mpRecord);

              _dbContext.SaveChanges();
            }

            // Load QA Queue Records --------------------------

            foreach(string id in item.testSumIds){
              TestSummary ts = _dbContext.TestSummaries.Find(id);
              ts.EvalStatus = "INQ";
              _dbContext.TestSummaries.Update(ts);

              Evaluation tsRecord = new Evaluation();
              tsRecord.EvaluationSetId = set_id.ToString();
              tsRecord.ProcessCode = "QA";
              
              if(item.submitMonPlan == false){
                tsRecord.StatusCode = "QUEUED";
              }else{
                tsRecord.StatusCode = "PENDING";
              }
              tsRecord.TestSumId = id;
              tsRecord.SubmittedOn = currentEasternTime; 
              _dbContext.Evaluations.Add(tsRecord);

              _dbContext.SaveChanges();
            }

            foreach(string id in item.qceIds){
              CertEvent ce = _dbContext.CertEvents.Find(id);
              ce.EvalStatus = "INQ";
              _dbContext.CertEvents.Update(ce);

              Evaluation ceRecord = new Evaluation();
              ceRecord.EvaluationSetId = set_id.ToString();
              ceRecord.ProcessCode = "QA";
              
              if(item.submitMonPlan == false){
                ceRecord.StatusCode = "QUEUED";
              }else{
                ceRecord.StatusCode = "PENDING";
              }
              ceRecord.QaCertEventId = id;
              ceRecord.SubmittedOn = currentEasternTime; 
              _dbContext.Evaluations.Add(ceRecord);

              _dbContext.SaveChanges();
            }

            foreach(string id in item.teeIds){
              TestExtensionExemption tee = _dbContext.TestExtensionExemptions.Find(id);
              tee.EvalStatus = "INQ";
              _dbContext.TestExtensionExemptions.Update(tee);

              Evaluation teeRecord = new Evaluation();
              teeRecord.EvaluationSetId = set_id.ToString();
              teeRecord.ProcessCode = "QA";
              
              if(item.submitMonPlan == false){
                teeRecord.StatusCode = "QUEUED";
              }else{
                teeRecord.StatusCode = "PENDING";
              }
              teeRecord.TeeId = id;
              teeRecord.SubmittedOn = currentEasternTime; 
              _dbContext.Evaluations.Add(teeRecord);

              _dbContext.SaveChanges();
            }

            //----------------------------------------------

            // Load Emission Queue Records -----------------
            foreach (string periodAbr in item.emissionsReportingPeriods){
              /*
              ReportingPeriod rp =  _dbContext.ReportingPeriods
                                .Where(rp => rp.PeriodAbbreviation == periodAbr)
                                .FirstOrDefault();
              
              
              EmissionEvaluation ee = _dbContext.EmissionEvaluations.Find(item.monPlanId, rp.ReportingPeriodId);
              ee.EvalStatus = "INQ";
              _dbContext.EmissionEvaluations.Update(ee);

              Evaluation emissionRecord = new Evaluation();
              emissionRecord.EvaluationSetId = set_id.ToString();
              emissionRecord.ProcessCode = "EM";
              
              if(item.submitMonPlan == false && (item.testSumIds.Count == 0 && item.qceIds.Count == 0 && item.teeIds.Count == 0)){
                emissionRecord.StatusCode = "QUEUED";
              }else{
                emissionRecord.StatusCode = "PENDING";
              }
              emissionRecord.RptPeriod = rp.ReportingPeriodId;
              emissionRecord.SubmittedOn = currentEasternTime; 
              _dbContext.Evaluation.Add(emissionRecord);

              _dbContext.SaveChanges();  
              */              
            }
          }catch(Exception e){
            Console.WriteLine(e.Message);
          }
      }
      return CreatedAtAction("BulkEvaluationResponse", new
      {          
        userId = request.UserId,
        userEmail = request.UserEmail,
        submittedOn = Utils.getCurrentEasternTime()
      });
    }
  }
}
