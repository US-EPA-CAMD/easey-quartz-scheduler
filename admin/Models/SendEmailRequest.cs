namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class SendEmailRequest
  {
    public string ToEmail { get; set; }
    public string FromEmail { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
  }
}