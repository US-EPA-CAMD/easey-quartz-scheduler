using DatabaseAccess;
using Epa.Camd.Logger;
using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Configuration;

class Program
{
    static Program()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        ConfigureLogging();

        // Initialize logger for this class
        _logger = LoggerProvider.GetLogger<Program>();

        // Build configuration from environment variables
        _configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
    }


    static void Main(string[] args)
    {
        string? jobName = ((args != null) && (args.Length >= 1)) ? args[0] : null;

        switch (jobName)
        {
            case "DataMartRefreshJob":
                {
                    var jobLogger = LoggerProvider.GetLogger<DataMartRefreshJob>();
                    var dbLogger = LoggerProvider.GetLogger<NpgSqlContext>();

                    var dbOptions = new DbContextOptionsBuilder<NpgSqlContext>()
                        .UseNpgsql(DBCredentials.CheckEngineRunnerDBConnectionStr)
                        .Options;

                    DataMartRefreshJob job = new (
                        new NpgSqlContext(_configuration, dbLogger, dbOptions),
                        jobLogger);

                    job.Execute(context: null!).GetAwaiter().GetResult();
                }
                break;
            default:
                {
                    if (string.IsNullOrWhiteSpace(jobName))
                        _logger.LogInformation("No job name provided. Exiting.");
                    else
                        _logger.LogInformation("Unknown job name provided: {JobName}. Exiting.", jobName);
                }
                break;
        }
    }


    private static readonly ILogger<Program> _logger;
    private static readonly IConfiguration _configuration;

    static class DBCredentials
    {
        static DBCredentials()
        {
            _configuration = Program._configuration;
            _connectionString = ConnectionStringManager.getConnectionString(Program._configuration);

            var dbInfo = new
            {
                Host = _configuration["EASEY_DB_HOST"] ?? "localhost",
                ReplicaHost = _configuration["EASEY_DB_REPLICA_HOST"] ?? "(none)",
                Port = _configuration["EASEY_DB_PORT"] ?? "5432",
                User = _configuration["EASEY_DB_USER"] ?? "postgres",
                Database = _configuration["EASEY_DB_NAME"] ?? "postgres",
                ReplicaEnabled = _configuration["EASEY_QUARTZ_SCHEDULER_DB_REPLICA_ENABLED"] ?? "false"
            };

            _logger.LogInformation("Database connection details (excluding password): {@DbInfo}", dbInfo);
        }

        private static readonly IConfiguration _configuration;
        private static readonly string _connectionString;

        public static string CheckEngineRunnerDBConnectionStr => _connectionString;
    }

    private static void ConfigureLogging()
    {
        try
        {
            DotNetEnv.Env.Load(); //Loads from .env if present
            var logLevelEnv = Environment.GetEnvironmentVariable("EASEY_LOG_LEVEL") ?? "Information";
            var parsedLevel = Enum.TryParse(logLevelEnv, true, out LogEventLevel logLevel)
                ? logLevel
                : LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(parsedLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

            // Configure LoggerProvider
            var factory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            Epa.Camd.Logger.LoggerProvider.Configure(factory);
        }
        catch (Exception ex)
        {
            //Keep this logging as Console logging. It is possible that Logger configuration was not successful.
            Console.WriteLine("FATAL: Logging configuration failed: " + ex.Message);
            Console.WriteLine(ex);
        }
    }

}