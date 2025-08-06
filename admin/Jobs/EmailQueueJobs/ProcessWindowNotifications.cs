using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class ProcessWindowNotifications : BaseEmailProcessor, IJob
    {
        public ProcessWindowNotifications(NpgSqlContext dbContext, IConfiguration configuration, ILogger<ProcessWindowNotifications> logger)
            : base(dbContext, configuration, logger)
        {
        }

        protected override string GetEmailTypeForRecipientApi()
        {
            return WINDOW_NOTIFICATION_EMAIL_TYPE_FOR_RECIP_API;
        }
        
        protected override string GetEmailTypeForDatabase()
        {
            return WINDOW_NOTIFICATION_EMAIL_TYPE_FOR_DB;
        }

        protected override string GetJobName()
        {
            return "ProcessWindowNotifications";
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ProcessEmailRecipients();
        }
    }
}