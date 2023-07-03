namespace Epa.Camd.Quartz.Scheduler.Models
{

  public class Recipient{
    public string[] emailAddressList {get; set; }
    public long[] plantIdList {get; set; }

  }

  public class RecipientResponse
  {
    public Recipient[] recipientList {get; set; }    
  }
}