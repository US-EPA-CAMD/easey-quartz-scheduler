using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFile : IJob
  {
    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Bulk Data File";
      public static readonly string JobDescription = "Streams a file to an S3 bucket";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkDataFile>(WithJobKey(), Identity.JobDescription);
    }

    public BulkDataFile(NpgSqlContext dbContext)
    {
      _dbContext = dbContext;
    }

    public Task Execute(IJobExecutionContext context)
    {
      try
      {
        LogHelper.info("Fired Bulk Data File Job");

        Console.Write("Fired Bulk File Process!");

        /*
        try
        {
          var sql_command = "DELETE FROM camdecmpswks.user_session WHERE token_expiration < now()";

          _dbContext.ExecuteSql(sql_command);
        }
        catch (Exception e)
        {
          LogHelper.error(e.Message, new LogVariable("stack", e.StackTrace));
        }

        LogHelper.info("Executed RemoveExpiredUserSession job successfully");
        */
        return Task.CompletedTask;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return null;
      }
    }

    public static IJobDetail CreateJobDetail()
    {
      return JobBuilder.Create<BulkDataFile>().Build();
    }

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

  }
}