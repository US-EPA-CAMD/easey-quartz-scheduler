using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;
using Microsoft.Extensions.Logging;
using Epa.Camd.Easey.Logging;

namespace Epa.Camd.Easey.JobScheduler.Jobs{
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
            try{
                var sql_command = "DELETE FROM camdecmpswks.user_check_out WHERE last_activity > now() + interval '2' minute";

                _dbContext.ExecuteSql(sql_command);
            }
            catch(Exception e){
                LogHelper.error(_logger, e.Message, new LogVariable("stack", e.StackTrace));
            }

            LogHelper.info(_logger, "Executed RemoveExpiredCheckoutRecord job successfully");
            return Task.CompletedTask;
        }
    }
}