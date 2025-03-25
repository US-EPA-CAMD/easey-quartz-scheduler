
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Quartz;
using Quartz.Impl;
using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CheckEngineRunner
{
    static class CheckEngineRunnerDBCredentials
    {
        private static string dbName = Environment.GetEnvironmentVariable("EASEY_DB_NAME");
        private static string dbPort = Environment.GetEnvironmentVariable("EASEY_DB_PORT");
        private static string dbUser = Environment.GetEnvironmentVariable("EASEY_DB_USER");
        private static string dbPwd = Environment.GetEnvironmentVariable("EASEY_DB_PWD");

        private static string dbConnString = "server = localhost; port = " + dbPort + "; user id = " + dbUser + "; password = " + dbPwd + "; database = " + dbName + "; pooling = true";

        public static string CheckEngineRunnerDBConnectionStr { get { return dbConnString; } }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            ConfigureLogging();

            string batchId = Guid.NewGuid().ToString();

            string fileTypeCd = ((args != null) && (args.Length >= 1)) ? args[0] : null;

            string localDir = System.IO.Directory.GetCurrentDirectory();
            string baseDir = localDir.Substring(0, localDir.IndexOf("CheckEngine") + 11);

            switch (fileTypeCd)
            {
                case "MP":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;

                        string dllPath = Path.Combine(baseDir, "MonitorPlan", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_MpReport(monPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, batchId);
                    }
                    break;

                case "EM":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string rpPeriodIdText = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        int rptPeriodId;
                        {
                            if (!int.TryParse(rpPeriodIdText, out rptPeriodId)) { rptPeriodId = 0; }
                        }

                        string dllPath = Path.Combine(baseDir, "Emissions", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_EmReport(monPlanId, rptPeriodId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;

                case "QAT":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string testSumId = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);


                        bool result = checkEngine.RunChecks_QaReport_Test(testSumId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;

                case "QCE":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string qaCertEventId = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);


                        bool result = checkEngine.RunChecks_QaReport_Qce(qaCertEventId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;
                case "TEE":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string teeId = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_QaReport_Tee(teeId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;
            }

            Console.WriteLine("Check Engine run completed");
            Console.ReadLine();
        }

        private static void ConfigureLogging()
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL: Logging configuration failed: " + ex.Message);
                Console.WriteLine(ex);
            }
        }
    }

    public class CheckEnginerJob : IJob
    {
        private static string connStr = CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr;
        
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;


            string batchId = Guid.NewGuid().ToString();

            string fileTypeCd = dataMap.GetString("fileTypeCd");
            string monPlanId = dataMap.GetString("monPlanId");

            string localDir = System.IO.Directory.GetCurrentDirectory();
            string baseDir = localDir.Substring(0, localDir.IndexOf("CheckEngine") + 11);


            switch (fileTypeCd)
            {
                case "MP":
                    {
                        string dllPath = Path.Combine(baseDir, "MonitorPlan", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_MpReport(monPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, batchId);
                        await Task.CompletedTask;
                    }
                    break;

                case "QAT":
                    {
                        string testSumId = dataMap.GetString("otherId");

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "net8.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_QaReport_Test(testSumId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                        await Task.CompletedTask;
                    }
                    break;

                case "QCE":
                    {
                        string qaCertEventId = dataMap.GetString("otherId");

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "netcoreapp3.1") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_QaReport_Qce(qaCertEventId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                        await Task.CompletedTask;
                    }
                    break;

                case "TEE":
                    {
                        string testExtenstionExemptionId = dataMap.GetString("otherId");

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "netcoreapp3.1") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_QaReport_Tee(testExtenstionExemptionId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                        await Task.CompletedTask;
                    }
                    break;
            }

        }

    }
}
