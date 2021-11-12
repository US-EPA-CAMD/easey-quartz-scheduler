using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;
using Microsoft.Extensions.Logging;
using Epa.Camd.Easey.Logging;

namespace Epa.Camd.Easey.JobScheduler.Jobs{
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
    }
}