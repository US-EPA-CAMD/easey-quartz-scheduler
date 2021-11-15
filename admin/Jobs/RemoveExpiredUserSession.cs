using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Quartz;
using Epa.Camd.Easey.JobScheduler.Models;
using Epa.Camd.Easey.JobScheduler.Logging;

namespace Epa.Camd.Easey.JobScheduler.Jobs
{
    public class RemoveExpiredUserSession : IJob
    {
        private NpgSqlContext _dbContext = null;
        private readonly ILogger _logger;

        public RemoveExpiredUserSession(NpgSqlContext dbContext, ILogger<RemoveExpiredUserSession> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            LogHelper.info(_logger, "Executing RemoveExpiredUserSession job");
            try{
                var sql_command = "DELETE FROM camdecmpswks.user_session WHERE token_expiration < now()";

                _dbContext.ExecuteSql(sql_command);
            }
            catch(Exception e){
                LogHelper.error(_logger, e.Message, new LogVariable("stack", e.StackTrace));
            }

            LogHelper.info(_logger, "Executed RemoveExpiredUserSession job successfully");
            return Task.CompletedTask;
        }

        public static JobKey WithJobKey()
        {
            return new JobKey(
                Constants.JobDetails.EXPIRED_USER_SESSIONS_KEY,
                Constants.JobDetails.EXPIRED_USER_SESSIONS_GROUP
            );
        }

        public static TriggerKey WithTriggerKey()
        {
            return new TriggerKey(
                Constants.TriggerDetails.EXPIRED_USER_SESSIONS_KEY,
                Constants.TriggerDetails.EXPIRED_USER_SESSIONS_GROUP
            );
        }

        public static IJobDetail WithJobDetail()
        {
            return JobBuilder.Create<RemoveExpiredUserSession>()
                .WithIdentity(
                    RemoveExpiredUserSession.WithJobKey()
                )
                .WithDescription(
                    Constants.JobDetails.EXPIRED_USER_SESSIONS_DESCRIPTION
                )
                .Build();
        }        

        public static TriggerBuilder WithCronSchedule(string cronExpression)
        {
            return TriggerBuilder.Create()
                .WithIdentity(
                    RemoveExpiredUserSession.WithTriggerKey()
                )
                .WithDescription(
                    Constants.TriggerDetails.EXPIRED_USER_SESSIONS_DESCRIPTION
                )
                .WithCronSchedule(cronExpression);
        }
    }
}