using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;
using SilkierQuartz;

namespace Epa.Camd.Quartz.Scheduler.Jobs.EmailQueueJobs
{
    public class ProcessWindowNotifications : BaseEmailProcessor, IJob
    {
        private static readonly string GROUP = Constants.QuartzGroups.MAINTAINANCE;
        private static readonly string JOB_NAME = "ProcessWindowNotifications";
        private static readonly string JOB_DESCRIPTION = "Sends submission window notification emails.";
        
        public ProcessWindowNotifications(NpgSqlContext dbContext, IConfiguration configuration, ILogger<ProcessWindowNotifications> logger)
            : base(dbContext, configuration, logger)
        {
        }

        public static void RegisterWithQuartz(IServiceCollection services)
        {
            services.AddQuartzJob<ProcessWindowNotifications>(WithJobKey(), JOB_DESCRIPTION);
        }
        
        private static JobKey WithJobKey()
        {
            return new JobKey(JOB_NAME, GROUP);
        }

        protected override string GetEmailTypeForRecipientApi()
        {
            return WindowNotificationEmailTypeForRecipientApi;
        }
        
        protected override string GetEmailTypeForDatabase()
        {
            return WindowNotificationEmailTypeForDb;
        }

        protected override string GetJobName()
        {
            return JOB_NAME;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ProcessEmailRecipients();
        }
    }
}