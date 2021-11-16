using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class RemoveExpiredCheckoutRecord : IJob
    {
        private NpgSqlContext _dbContext = null;
        private readonly ILogger _logger;

        public RemoveExpiredCheckoutRecord(NpgSqlContext dbContext, ILogger<RemoveExpiredCheckoutRecord> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            LogHelper.info(_logger, "Executing RemoveExpiredCheckoutRecord job");
            try
            {
                JobDataMap dataMap = context.MergedJobDataMap;
                int interval;
                if(dataMap.Contains("Interval"))
                    interval = dataMap.GetInt("Interval");
                else
                    interval = 2;

                var sql_command = "DELETE FROM camdecmpswks.user_check_out WHERE last_activity > now() + interval '" + interval.ToString() + "' minute";

                _dbContext.ExecuteSql(sql_command);
            }
            catch(Exception e)
            {
                LogHelper.error(_logger, e.Message, new LogVariable("stack", e.StackTrace));
            }

            LogHelper.info(_logger, "Executed RemoveExpiredCheckoutRecord job successfully");
            return Task.CompletedTask;
        }

        public static JobKey WithJobKey()
        {
            return new JobKey(
                Constants.JobDetails.EXPIRED_CHECK_OUTS_KEY,
                Constants.JobDetails.EXPIRED_CHECK_OUTS_GROUP
            );
        }

        public static TriggerKey WithTriggerKey()
        {
            return new TriggerKey(
                Constants.TriggerDetails.EXPIRED_CHECK_OUTS_KEY,
                Constants.TriggerDetails.EXPIRED_CHECK_OUTS_GROUP
            );
        }

        public static IJobDetail WithJobDetail()
        {
            return JobBuilder.Create(typeof(RemoveExpiredCheckoutRecord))
                .WithIdentity(
                    RemoveExpiredCheckoutRecord.WithJobKey()
                )
                .WithDescription(
                    Constants.JobDetails.EXPIRED_CHECK_OUTS_DESCRIPTION
                )
                .Build();
        }        

        public static TriggerBuilder WithCronSchedule(string cronExpression)
        {
            return TriggerBuilder.Create()
                .WithIdentity(
                    RemoveExpiredCheckoutRecord.WithTriggerKey()
                )
                .WithDescription(
                    Constants.TriggerDetails.EXPIRED_CHECK_OUTS_DESCRIPTION
                )
                .WithCronSchedule(cronExpression);
        }

    }
}