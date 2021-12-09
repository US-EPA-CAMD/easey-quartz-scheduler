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
  public class RemoveExpiredUserSession : IJob
  {
    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Cleanup Expired User Sessions";
      public static readonly string JobDescription = "Deletes all records from camdecmpswks.user_session table where the token_expiration < current_time resulting in an invalid session.";
      public static readonly string TriggerName = "Cleanup Expired User Sessions every 15 mins";
      public static readonly string TriggerDescription = "Cleanup Expired User Sessions on the 15th, 30th, & 45th minute of each hour & on the hour.";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<RemoveExpiredUserSession>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<RemoveExpiredUserSession>(WithCronSchedule("0 0/5 * ? * * *"));
      }
    }

    public RemoveExpiredUserSession(NpgSqlContext dbContext)
    {
      _dbContext = dbContext;
    }

    public Task Execute(IJobExecutionContext context)
    {
      try
      {
        LogHelper.info("Executing RemoveExpiredUserSession job");
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
        return Task.CompletedTask;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return null;
      }
    }

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

    public static TriggerKey WithTriggerKey()
    {
      return new TriggerKey(Identity.TriggerName, Identity.Group);
    }

    public static IJobDetail WithJobDetail()
    {
      return JobBuilder.Create<RemoveExpiredUserSession>()
          .WithIdentity(WithJobKey())
          .WithDescription(Identity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithTriggerKey())
          .WithDescription(Identity.TriggerDescription)
          .WithCronSchedule(cronExpression);
    }
  }
}