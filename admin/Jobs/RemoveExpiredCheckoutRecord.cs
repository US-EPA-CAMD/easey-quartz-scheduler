using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class RemoveExpiredCheckoutRecord : IJob
  {
    private NpgSqlContext _dbContext = null;
    private readonly ILogger _logger;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
      public static readonly string JobName = "Cleanup Expired Check-Outs";
      public static readonly string JobDescription = "Deletes all records from camdecmpswks.user_check_out table where the current_time - last_activity is greater than the maximum inactivity period.";
      public static readonly string TriggerName = "Cleanup Expired Check-Outs every 15 minutes";
      public static readonly string TriggerDescription = "Cleanup Expired Check-Outs on the 15th, 30th, & 45th minute of each hour & on the hour.";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<RemoveExpiredCheckoutRecord>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<RemoveExpiredCheckoutRecord>(WithCronSchedule("0 0/5 * ? * * *"));
      }
    }

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
        if (dataMap.Contains("Interval"))
          interval = dataMap.GetInt("Interval");
        else
          interval = 2;

        //DELETE FROM camdecmpswks.user_check_out WHERE last_activity < now() - interval '2' minute
        var sql_command = "DELETE FROM camdecmpswks.user_check_out WHERE last_activity < now() - interval '" + interval.ToString() + "' minute";

        _dbContext.ExecuteSql(sql_command);
      }
      catch (Exception e)
      {
        LogHelper.error(_logger, e.Message, new LogVariable("stack", e.StackTrace));
      }

      LogHelper.info(_logger, "Executed RemoveExpiredCheckoutRecord job successfully");
      return Task.CompletedTask;
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
      return JobBuilder.Create<RemoveExpiredCheckoutRecord>()
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