using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFileMaintenance : IJob
  {
    private NpgSqlContext _dbContext = null;

    public IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Bulk Data File Maintenance";
      public static readonly string JobDescription = "Run a check on the bulk data file maintenance queue";
      public static readonly string TriggerName = "Run nightly and check which files need to have maintenance performed";
      public static readonly string TriggerDescription = "Run nightly and check which files need to get rerun or cleaned up";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkDataFileMaintenance>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<BulkDataFileMaintenance>(WithCronSchedule("0 0 * * * ?"));
      }
    }

    public BulkDataFileMaintenance(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      /*
      try
      {
        LogHelper.info("Executing BulkDataFileMaintenance job");

        List<QuartzBulkDataFile> maintenance_files = await _dbContext.ExecuteBulkDataFileQuery(@"select bdf.*
            from camdaux.qrtz_bulk_data_file_queue bdf
            where is_valid = 'Y' and (
            	status_cd = 'ERROR' or (
            		(status_cd = 'QUEUED' or status_cd = 'WIP') and
            		current_timestamp >= bdf.add_date + interval '24 hours'
            	)
            )
            order by year, quarter, state_cd");

        maintenance_files.ForEach(file => {
            if(file.StatusCd == "ERROR" && file.StatusCd == "Y"){
               file.StatusCd = "N"; 
            }
            else if((file.StatusCd == "WIP" || file.StatusCd == "QUEUED") && file.StatusCd == "Y"){
                Console.Write(context.Scheduler.DeleteJob(new JobKey(file.JobId.ToString(), Constants.QuartzGroups.BULK_DATA)));
            }
        });

        LogHelper.info("Executed BulkDataFileMaintenance job successfully");
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
      }
      */
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
      return JobBuilder.Create<BulkDataFileMaintenance>()
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