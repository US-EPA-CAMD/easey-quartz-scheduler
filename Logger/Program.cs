using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Epa.Camd.Quartz.Scheduler
{
  public class Program
  {
    public static void Main(string[] args)
    {
      
      Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .CreateLogger();
      
    }
  }
}
