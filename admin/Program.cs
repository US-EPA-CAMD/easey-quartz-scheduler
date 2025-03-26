using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SilkierQuartz;

namespace Epa.Camd.Quartz.Scheduler
{
  public class Program
  {
    public static int Main(string[] args)
    {
      try
      {
          DotNetEnv.Env.Load(); //Loads from .env if present

          var logLevelEnv = Environment.GetEnvironmentVariable("LOGGING__LEVEL") ?? "Information";
          var parsedLevel = Enum.TryParse(logLevelEnv, true, out LogEventLevel logLevel)
            ? logLevel
            : LogEventLevel.Information;

          Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(parsedLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateLogger();

            Log.Information("Starting web host");
            CreateHostBuilder(args).Build().Run();
            return 0;
      }
      catch (Exception ex)
      {
            Console.WriteLine("FATAL: Logging configuration failed: " + ex.Message);
            Console.WriteLine(ex);
        return 1;
      }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, logging) =>
              {
                // Create the factory and wire it to LoggerProvider
                var factory = LoggerFactory.Create(builder =>
                {
                  builder.ClearProviders();
                  builder.AddSerilog(); // use the Serilog pipeline already built
                });

                Epa.Camd.Logger.LoggerProvider.Configure(factory);
            })
            .UseSerilog() // required to wire Serilog to Microsoft.Extensions.Logging
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
            })
            .ConfigureSilkierQuartzHost();
  }
}
