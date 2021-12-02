using System.Threading.Tasks;
using System;
using Quartz;
using Epa.Camd.Easey.RulesApi.Models;
using SilkierQuartz;

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

            string tableName = dataMap.GetString("tableName");
            string columnName = dataMap.GetString("columnName");
            string condition = dataMap.GetString("condition");

            var sql_command = $"DELETE FROM camdecmpswks.{tableName} WHERE {columnName} {condition}";
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
