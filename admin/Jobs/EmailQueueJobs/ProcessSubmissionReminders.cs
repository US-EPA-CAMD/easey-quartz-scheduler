using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;
using SilkierQuartz;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class ProcessSubmissionReminders : BaseEmailProcessor, IJob
    {
        private static readonly string GROUP = Constants.QuartzGroups.MAINTAINANCE;
        private static readonly string JOB_NAME = "ProcessSubmissionReminders";
        private static readonly string JOB_DESCRIPTION = "Sends submission reminder emails.";
        
        public ProcessSubmissionReminders(NpgSqlContext dbContext, IConfiguration configuration, ILogger<ProcessSubmissionReminders> logger)
            : base(dbContext, configuration, logger)
        {
        }
        
        public static void RegisterWithQuartz(IServiceCollection services)
        {
            services.AddQuartzJob<ProcessSubmissionReminders>(WithJobKey(), JOB_DESCRIPTION);
        }
        
        private static JobKey WithJobKey()
        {
            return new JobKey(JOB_NAME, GROUP);
        }

        protected override string GetEmailTypeForRecipientApi()
        {
            return SUBMISSION_REMINDER_EMAIL_TYPE_FOR_RECIP_API;
        }
        
        protected override string GetEmailTypeForDatabase()
        {
            return SUBMISSION_REMINDER_EMAIL_TYPE_FOR_DB;
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