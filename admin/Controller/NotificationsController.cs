using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using SilkierQuartz;
using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler
{
  [ApiController()]
  [Produces("application/json")]
  [Route("quartz-mgmt/triggers/notifications")]
  public class NotificationsController : ControllerBase
  {
    private NpgSqlContext _dbContext = null;
    private IConfiguration Configuration { get; }

    public NotificationsController(
      NpgSqlContext dbContext,
      IConfiguration configuration
    )
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    [HttpPost("emails")]
    public async Task<ActionResult> SendEmail([FromBody] SendEmailRequest request)
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
  }
}
