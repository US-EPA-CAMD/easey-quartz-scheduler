using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;

    public class RemoveExpiredRecord : IJob
    {

        private NpgSqlContext _dbContext = null;

        public RemoveExpiredRecord(NpgSqlContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try{
                JobDataMap dataMap = context.JobDetail.JobDataMap;

                string Schema = dataMap.GetString("Schema");
                string Table = dataMap.GetString("Table");
                string Condition = dataMap.GetString("Condition");

                var sql_command = $"DELETE FROM {Schema}.{Table} WHERE {Condition}";
                Console.WriteLine(sql_command);

                _dbContext.ExecuteSql(sql_command);

                Console.WriteLine("Deleted old records");
            }
            catch(Exception e){
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
