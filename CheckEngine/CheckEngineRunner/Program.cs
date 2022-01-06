
using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;

namespace CheckEngineRunner
{
    class Program
    {
        private static string dbName = Environment.GetEnvironmentVariable("EASEY_DB_NAME", EnvironmentVariableTarget.Machine);
        private static string dbPort = Environment.GetEnvironmentVariable("EASEY_DB_PORT", EnvironmentVariableTarget.Machine);
        private static string dbUser = Environment.GetEnvironmentVariable("EASEY_DB_USER", EnvironmentVariableTarget.Machine);
        private static string dbPwd = Environment.GetEnvironmentVariable("EASEY_DB_PWD", EnvironmentVariableTarget.Machine);

        private static string con = "server = localhost; port = "+dbPort+"; user id = "+dbUser+"; password = "+dbPwd+"; database = "+dbName+"; pooling = true";

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
                 .UsingJobData("ProcessCode", "MP")
                 .UsingJobData("connectionString", con)
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
        private static string dbName = Environment.GetEnvironmentVariable("EASEY_DB_NAME", EnvironmentVariableTarget.Machine);
        private static string dbPort = Environment.GetEnvironmentVariable("EASEY_DB_PORT", EnvironmentVariableTarget.Machine);
        private static string dbUser = Environment.GetEnvironmentVariable("EASEY_DB_USER", EnvironmentVariableTarget.Machine);
        private static string dbPwd = Environment.GetEnvironmentVariable("EASEY_DB_PWD", EnvironmentVariableTarget.Machine);

        private static string con = "server = localhost; port = " + dbPort + "; user id = " + dbUser + "; password = " + dbPwd + "; database = " + dbName + "; pooling = true";
        
        public async Task Execute(IJobExecutionContext context)
        {
            string localDir = System.IO.Directory.GetCurrentDirectory();
            string dllPath = localDir.Substring(0, localDir.IndexOf("CheckEngine") + 11) + "\\MonitorPlan\\obj\\Debug\\netcoreapp3.1\\";
            Console.WriteLine(con);
            cCheckEngine checkEngine = new cCheckEngine("userId", con, con, con, dllPath, "dumpfilePath", 20);
  
            bool result = checkEngine.RunChecks_MpReport("MDC-97B373B8EC1245EB986354DCC390693D", new DateTime(2008, 1, 1), DateTime.Now.AddYears(1), eCheckEngineRunMode.Normal);
            await Task.CompletedTask;
        }     
    }
}
