using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;

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
      Services services = (Services)Request.HttpContext.Items[typeof(Services)];
      
      await SendMail.StartNow(
        services.Scheduler,
        request.ToEmail,
        request.FromEmail,
        request.Subject,
        request.Message,
        request.Purpose,
        Configuration["EmailSettings:EASEY_QUARTZ_SMTP_HOST"],
        Configuration["EmailSettings:EASEY_QUARTZ_SMTP_PORT"]
      );

      return CreatedAtAction("SendEmailResponse", request);
    }
  }
}
