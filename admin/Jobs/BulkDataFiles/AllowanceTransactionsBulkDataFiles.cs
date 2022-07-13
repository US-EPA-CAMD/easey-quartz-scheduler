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
  public class AllowanceTransactionsBulkDataFiles : IJob
  {

    private Guid job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public IConfiguration Configuration { get; }

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Allowance Transactions Bulk Data";
      public static readonly string JobDescription = "Determine which allowance transactions need to be regenerated and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run nightly and check which allowance transaction attributes files need to be regenerated";
      public static readonly string TriggerDescription = "Runs nightly to determine if files need to be regenerated based on query results";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<AllowanceTransactionsBulkDataFiles>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<AllowanceTransactionsBulkDataFiles>(WithCronSchedule("* 0/10 1-5 30 1 ? *"));
      }
    }

    public AllowanceTransactionsBulkDataFiles(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      LogHelper.info("Executing AllowanceTransactionsBulkDataFiles job");

      // Does this job already exist? Otherwise create and schedule a new copy
      List<List<Object>> jobAlreadyExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name = 'Allowance Transactions' AND add_date::date = now()::date;", 9);
      if(jobAlreadyExists.Count != 0){
        return; // Job already exists , do not run again
      }

      // Does data mart nightly exists for current date and has it completed
      if(Configuration["EASEY_DATAMART_BYPASS"] != "true"){
        List<List<Object>> datamartExists = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.job_log WHERE job_name in ('Datamart Nightly') AND add_date::date = now()::date AND end_date IS NOT NULL;", 9);
        if(datamartExists.Count == 0){
          return;
        }
      }

      JobLog jl = new JobLog(); 

      try
      {

        jl.JobId = job_id;
        jl.JobSystem = "Quartz";
        jl.JobClass = "Bulk Data File";
        jl.JobName = "Allowance Transactions";
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = Utils.getCurrentEasternTime();
        jl.EndDate = null;
        jl.StatusCd = "WIP";

        _dbContext.JobLogs.Add(jl);
        await _dbContext.SaveChangesAsync();
        
        List<List<Object>> rowsPerPrg = await _dbContext.ExecuteSqlQuery("SELECT * FROM camdaux.vw_allowance_transactions_bulk_files_to_generate", 1);
        
        
        for(int row = 0; row < rowsPerPrg.Count; row++){
          string code = (string) rowsPerPrg[row][0];
          decimal year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "transactionBeginDate=" + year + "-01-01&transactionEndDate=" + year + "-12-31&programCodeInfo=" + code;

          BulkFileJobQueue.AddBulkDataJobToQueue(await _dbContext.CreateBulkFileJob(year, null, null, "Allowance", null, Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-transactions?" + urlParams, "allowance/transactions-" + code.ToLower() + ".csv", job_id, code));
        }
        
        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
        _dbContext.JobLogs.Update(jl);
        await _dbContext.SaveChangesAsync();
        LogHelper.info("Executing AllowanceTransactionsBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        jl.StatusCd = "ERROR";
        jl.EndDate = Utils.getCurrentEasternTime();
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
      return JobBuilder.Create<AllowanceTransactionsBulkDataFiles>()
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