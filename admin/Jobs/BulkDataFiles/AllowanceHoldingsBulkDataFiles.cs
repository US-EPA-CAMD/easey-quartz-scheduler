using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class AllowanceHoldingsBulkDataFiles : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;
    private readonly ILogger<AllowanceHoldingsBulkDataFiles> _logger;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Allowance Holdings Bulk Data";
      public static readonly string JobDescription = "Generate Allowance Holdings and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run nightly and regen allowance holdings files";
      public static readonly string TriggerDescription = "Runs nightly to generate allowance holdings files";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<AllowanceHoldingsBulkDataFiles>(WithJobKey(), Identity.JobDescription);
    }

    public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      try {
        JobKey jobKey = WithJobKey();
        string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_ALLOWANCE_HOLDINGS_SCHEDULE"] ?? "0 0/10 2-4 ? * * *";
        TriggerBuilder triggerBuilder = WithCronSchedule(cronExpression);

        if (await scheduler.CheckExists(jobKey)) {
          ITrigger trigger = await scheduler.GetTrigger(WithTriggerKey());

          if (
            trigger is ICronTrigger cronTrigger &&
            cronTrigger.CronExpressionString != cronExpression
          ) {
            await scheduler.RescheduleJob(WithTriggerKey(), triggerBuilder.Build());
            Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
          }
        } else {
          app.UseQuartzJob<AllowanceHoldingsBulkDataFiles>(triggerBuilder);
          Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
        }
      } catch(Exception e) {
        Console.WriteLine("ERROR");
        Console.WriteLine(e.Message);
      }
    }

    public AllowanceHoldingsBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration, ILogger<AllowanceHoldingsBulkDataFiles> logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      _logger.LogInformation("Executing AllowanceHoldingsBulkDataFiles job. JobId: {JobId}", job_id);

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Holdings' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        _logger.LogInformation("Job already exists for today. Skipping execution. JobId: {JobId}", job_id);
        return; // Job already exists , do not run again
      }
      
      // Does data mart nightly exists for current date and has it completed
      if(Utils.Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          _logger.LogInformation("Datamart nightly job has not completed. Skipping execution. JobId: {JobId}", job_id);
          return;
        }
      }

      _logger.LogInformation("Creating Allowance Holdings JobLog. JobId: {JobId}", job_id);

      JobLog jl = new JobLog();

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Holdings";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> programCodeRows = await this._dbContext.ExecuteSqlQuery("SELECT prg_cd FROM camdmd.program_code pc WHERE pc.bulk_file_active = 1", 1);
        string[] programCodes = new string[programCodeRows.Count];

        for(int i = 0; i < programCodeRows.Count; i++){
          programCodes[i] = (string) programCodeRows[i][0];
        }
        
        for(int row = 0; row < programCodes.Length; row++){
          string code = programCodes[row];
          decimal year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "programCodeInfo=" + code;

          _logger.LogInformation("Creating bulk file record for programCode: {ProgramCode}", code);
          await _dbContext.CreateBulkFileRecord("Allowance-Holdings-" + code, job_id,null, null, null, "Allowance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-holdings?" + urlParams, "allowance/holdings-" + code.ToLower() + ".csv", job_id, code);
        }
        
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("{JobName} job completed successfully. JobId: {JobId}", nameof(AllowanceHoldingsBulkDataFiles), job_id);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Error executing {JobName}. JobId: {JobId}, Error: {ErrorMessage}", nameof(AllowanceHoldingsBulkDataFiles),
                job_id,
                e.Message ?? "No message");

        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
        jl.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        _logger.LogError(e, "Error executing {JobName}. JobId: {JobId}, Error: {ErrorMessage}", nameof(AllowanceHoldingsBulkDataFiles),
                        job_id,
                        e.Message ?? "No message");
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
      return JobBuilder.Create<AllowanceHoldingsBulkDataFiles>()
          .WithIdentity(WithJobKey())
          .WithDescription(Identity.JobDescription)
          .Build();
    }

    public static TriggerBuilder WithCronSchedule(string cronExpression)
    {
      return TriggerBuilder.Create()
          .WithIdentity(WithTriggerKey())
          .WithDescription(Identity.TriggerDescription)
          .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
    }
  }
}