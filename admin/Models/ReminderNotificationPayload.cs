namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class ReminderNotificationPayload
  {
    public string userId {get; set;}

    public long? plantId {get; set; }

    public string submissionType {get; set;}

    public System.Nullable<bool> isMats {get; set;}

    public string emailType {get; set;}

    public long[] plantIdList {get; set; }    
  }
}