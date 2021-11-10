using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;
using Microsoft.Extensions.Logging;
using Epa.Camd.Easey.Logging;


namespace Epa.Camd.Easey.JobScheduler.Jobs
{
    public class HelloWorld : IJob
    {
        private NpgSqlContext _dbContext = null;
        private readonly ILogger _logger;

        public HelloWorld(NpgSqlContext dbContext, ILogger<HelloWorld> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            /*
            LogHelper.info(_logger, "Hello World", new LogVariable("UserId", "Kyle"), new LogVariable("start", DateTime.Now));
            LogHelper.error(_logger, "Errored Here");
            */
            
            try{
                JobDataMap dataMap = context.JobDetail.JobDataMap;

                string Name = dataMap.GetString("Name");
            }
            catch(Exception e){
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
}