
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Quartz;
using Quartz.Impl;
using System;
using System.IO;
using System.Threading.Tasks;

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

            /*
            string fileTypeCd = ((args != null) && (args.Length >= 1)) ? args[0] : null;
            string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
            string otherId = ((args != null) && (args.Length >= 3)) ? args[2] : null;


            // 1. Create a scheduler Factory
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            // 2. Get and start a scheduler
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            // 3. Create a job
            IJobDetail job = JobBuilder.Create<CheckEnginerJob>()

                .WithIdentity("Monitor Plan Evaluation", "DEFAULT")
                .UsingJobData("connectionString", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr)
                .UsingJobData("ProcessCode", "MP")
                .UsingJobData("fileTypeCd", fileTypeCd)
                .UsingJobData("monPlanId", monPlanId)
                .UsingJobData("otherId", otherId)
                .Build();

            // 4. Create a trigger
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("Monitor Plan Evaluation", "DEFAULT")
                .UsingJobData("MonitorPlanId", "request.MonitorPlanId")
                .UsingJobData("ConfigurationName", "request.ConfigurationName")
                .StartNow()
                .Build();

            // 5. Schedule the job using the job and trigger 
            await scheduler.ScheduleJob(job, trigger);

            Console.ReadLine();

            */

            string batchId = Guid.NewGuid().ToString();

            string fileTypeCd = ((args != null) && (args.Length >= 1)) ? args[0] : null;

            string localDir = System.IO.Directory.GetCurrentDirectory();
            string baseDir = localDir.Substring(0, localDir.IndexOf("CheckEngine") + 11);

            switch (fileTypeCd)
            {
                case "MP":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;

                        string dllPath = Path.Combine(baseDir, "MonitorPlan", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
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

                        string dllPath = Path.Combine(baseDir, "Emissions", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_EmReport(monPlanId, rptPeriodId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;

                case "QAT":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string testSumId = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);


                        bool result = checkEngine.RunChecks_QaReport_Test(testSumId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;

                case "QCE":
                    {
                        string monPlanId = ((args != null) && (args.Length >= 2)) ? args[1] : null;
                        string qaCertEventId = ((args != null) && (args.Length >= 3)) ? args[2] : null;

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr, dllPath, "dumpfilePath", 20);


                        bool result = checkEngine.RunChecks_QaReport_Qce(qaCertEventId, monPlanId, eCheckEngineRunMode.Normal, batchId);
                    }
                    break;
            }

            Console.ReadLine();
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
                        string dllPath = Path.Combine(baseDir, "MonitorPlan", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
                        cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);

                        bool result = checkEngine.RunChecks_MpReport(monPlanId, new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal, batchId);
                        await Task.CompletedTask;
                    }
                    break;

                case "QAT":
                    {
                        string testSumId = dataMap.GetString("otherId");

                        string dllPath = Path.Combine(baseDir, "QA", "obj", "Debug", "netcoreapp6.0") + Path.DirectorySeparatorChar;
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
