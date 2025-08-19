namespace Epa.Camd.Quartz.Scheduler.Models
{

  public class Recipient{
    public string emailAddressList {get; set; }  // Changed from string[] to string
    public long[] plantIdList {get; set; }

  }

  public class RecipientResponse
  {
    public Recipient[] recipients {get; set; }    // Changed from recipientList to recipients
    public bool hasError {get; set; }            // Added error handling fields
    public string errorMessage {get; set; }      // Added error message field
  }
}