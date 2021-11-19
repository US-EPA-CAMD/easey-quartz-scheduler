using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Logging;

namespace Epa.Camd.Quartz.Scheduler
{
  [ApiController()]
  [Route("quartz/api/triggers/notifications")]
  [Produces("application/json")]
  public class NotificationsController : ControllerBase
  {
    private readonly ILogger _logger;
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }

    public NotificationsController(
      NpgSqlContext dbContext,
      IConfiguration configuration,
      ILogger<CheckEngineEvaluation> logger
    ) {
      _logger = logger;
      _dbContext = dbContext;
      Configuration = configuration;
    }

    [HttpPost("emails")]
    public async Task<ActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
      string apiKey = Request.Headers["X-API-KEY"];

      if (
        apiKey != null &&
        (apiKey == Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY_ECMPS"] ||
         apiKey == Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY_CAMPD"])
      )
      {
        Services services = (Services)Request.HttpContext.Items[typeof(Services)];
        
        await SendMail.StartNow(
          request.ToEmail,
          request.FromEmail,
          request.Subject,
          request.Message,
          request.Purpose,
          services.Scheduler,
          Configuration
        );

        return CreatedAtAction("SendEmailResponse", request);
      }
      else
      {
        string message = "API Key is either missing or not an authorized client!";
        LogHelper.error(_logger, message, new LogVariable("API Key", apiKey), new LogVariable("Request", request));
        return BadRequest(message);
      }
    }
  }
}
