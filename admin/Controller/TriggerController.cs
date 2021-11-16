using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler
{
  [ApiController]
  [Route("quartz/api/triggers")]
  [Produces("application/json")]
  public class TriggerController : ControllerBase
  {
    private IConfiguration Configuration { get; }

    public TriggerController(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    private async Task<ActionResult> TriggerCheckEngineEvaluation(
      string processCode,
      EvaluationRequest request
    ) {
      Services Services = (Services)Request.HttpContext.Items[typeof(Services)];
      
      Guid id = Guid.NewGuid();
      DateTime submittedOn = DateTime.Now;

      await CheckEngineEvaluation.StartNow(
        Services.Scheduler,
        id,
        processCode,
        request.MonitorPlanId,
        request.UserId,
        submittedOn
      );

      return CreatedAtAction("EvaluationResponse)", new {
        id = id,
        processCode = processCode,
        monitorPlanId = request.MonitorPlanId,
        userId = request.UserId,
        submittedOn = submittedOn
      });
    }

    [HttpPost("monitor-plans")]
    public async Task<ActionResult> TriggerMPEvaluation([FromBody] EvaluationRequest request)
    {
      return await TriggerCheckEngineEvaluation("MP", request);
    }

    [HttpPost("qa-certifications")]
    public async Task<ActionResult> TriggerQAEvaluation([FromBody] EvaluationRequest request)
    {
      return await TriggerCheckEngineEvaluation("QA", request);
    }

    [HttpPost("emissions")]
    public async Task<ActionResult> TriggerEMEvaluation([FromBody] EvaluationRequest request)
    {
      return await TriggerCheckEngineEvaluation("EM", request);
    }

    [HttpPost("email")]
    public async Task<ActionResult> SendEmail([FromBody] SendEmailRequest request)
    {

      Services services = (Services)Request.HttpContext.Items[typeof(Services)];
      
      await SendMail.StartNow(
        services.Scheduler,
        request.ToEmail,
        request.FromEmail,
        request.Subject,
        request.Message,
        Configuration["EmailSettings:EASEY_QUARTZ_SMTP_HOST"],
        Configuration["EmailSettings:EASEY_QUARTZ_SMTP_PORT"]
      );

      return CreatedAtAction("SendEmailResponse", request);
    }
  }
}
