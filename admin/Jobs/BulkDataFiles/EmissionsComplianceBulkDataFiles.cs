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
  public class EmissionsComplianceBulkDataFiles  : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Emissions Compliance Bulk Data";
      public static readonly string JobDescription = "Determine which emissions compliance data needs to be regenerated and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run yearly and check which emissions compliance files need to be regenerated";
      public static readonly string TriggerDescription = "Runs nightly to determine if files need to be regenerated based on query results";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<EmissionsComplianceBulkDataFiles>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<EmissionsComplianceBulkDataFiles>(WithCronSchedule("0 0 0 15 1 ? *"));
      }
    }

    public EmissionsComplianceBulkDataFiles (NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing EmissionsComplianceBulkDataFiles job");

      JobLog jl = new JobLog(); 

      try
      {
        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Emissions Compliance";
        jl.AddDate = DateTime.Now;
        jl.StartDate = DateTime.Now;
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerPrg = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_emissions_based_compliance_bulk_files_to_generate", 1);
        
        if(rowsPerPrg.Count > 0){
          await context.Scheduler.ScheduleJob(await _dbContext.CreateBulkFileJob(null, null, null, "Emissions-Compliance", "Total", Configuration["EASEY_ACCOUNT_API"] + "/emissions-compliance/stream", "compliance/emissions-compliance-arpnox.csv", job_id, "ARP"), TriggerBuilder.Create().StartNow().Build());
        }
                
        jl.StatusCd = "COMPLETE";
        jl.EndDate = DateTime.Now;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executing EmissionsComplianceBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = DateTime.Now;
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.error(e.Message);
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
      return JobBuilder.Create<EmissionsComplianceBulkDataFiles>()
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