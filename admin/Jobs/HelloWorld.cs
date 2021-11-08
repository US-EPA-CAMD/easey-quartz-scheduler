using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;

namespace Epa.Camd.Easey.JobScheduler.Jobs
{
    public class HelloWorld : IJob
    {
        private NpgSqlContext _dbContext = null;

        public HelloWorld(NpgSqlContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try{
                JobDataMap dataMap = context.JobDetail.JobDataMap;

                string Name = dataMap.GetString("Name");

                Console.WriteLine("Hello " + Name);
            }
            catch(Exception e){
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
}