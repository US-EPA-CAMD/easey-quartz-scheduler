namespace Epa.Camd.Easey.JobScheduler.Logging
{
  public class LogVariable
  {

    public string key;
    public object value;
    public LogVariable(string key, object value)
    {
      this.key = key;
      this.value = value;
    }
  }
}