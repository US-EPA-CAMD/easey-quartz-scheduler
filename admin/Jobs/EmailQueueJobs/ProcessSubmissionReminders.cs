using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class ProcessSubmissionReminders : BaseEmailProcessor, IJob
    {
        public ProcessSubmissionReminders(NpgSqlContext dbContext, IConfiguration configuration, ILogger<ProcessSubmissionReminders> logger)
            : base(dbContext, configuration, logger)
        {
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
            return "ProcessSubmissionReminders";
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ProcessEmailRecipients();
        }
    }
}