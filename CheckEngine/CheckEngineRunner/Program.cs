
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Quartz;
using Quartz.Impl;
using System;
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
            // 1. Create a scheduler Factory
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            // 2. Get and start a scheduler
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            // 3. Create a job
            IJobDetail job = JobBuilder.Create<CheckEnginerJob>()
                    .WithIdentity("Monitor Plan Evaluation", "DEFAULT")
                 .UsingJobData("ProcessCode", "QA")
                 .UsingJobData("connectionString", CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr)
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

        }
    }

    public class CheckEnginerJob : IJob
    {
        private static string connStr = CheckEngineRunnerDBCredentials.CheckEngineRunnerDBConnectionStr;
        
        public async Task Execute(IJobExecutionContext context)
        {
            string localDir = System.IO.Directory.GetCurrentDirectory();
            string dllPath = localDir.Substring(0, localDir.IndexOf("CheckEngine") + 11) + "\\QA\\obj\\Debug\\netcoreapp3.1\\";
            //Console.WriteLine(connStr);
            cCheckEngine checkEngine = new cCheckEngine("userId", connStr, connStr, connStr, dllPath, "dumpfilePath", 20);
  
            //bool result = checkEngine.RunChecks_MpReport("SUPERMIKE-34FFE030555E4D5BB4DA7F6166F69B9F", new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal);
            bool result = checkEngine.RunChecks_QaReport_Test("ac6d2246-8e07-4cc2-a4d6-a4f776d5fb6a", "MDC-B4147730E09A4AEFBCFE88A76A60C840", eCheckEngineRunMode.Normal, "test");
            await Task.CompletedTask;
        }     
    }
}
