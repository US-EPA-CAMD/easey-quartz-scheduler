using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Logger;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class ApportionedEmissionsBulkData : IJob
  {

    private Guid parent_job_id = Guid.NewGuid();

    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Apportioned Emissions Bulk Data";
      public static readonly string JobDescription = "Determine which emissions need to be regenerated and schedule BulkDataFile jobs to handle the regen";
      public static readonly string TriggerName = "Run nightly and check which files need to be regenerated";
      public static readonly string TriggerDescription = "Runs nightly to determine if files need to be regenerated based on query results or end of reporting quarter";
    }

    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<ApportionedEmissionsBulkData>(WithJobKey(), Identity.JobDescription);
    }

    public static async void ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
    {
      if (!await scheduler.CheckExists(WithJobKey()))
      {
        app.UseQuartzJob<ApportionedEmissionsBulkData>(WithCronSchedule("0 0 * * * ?"));
      }
    }

    public ApportionedEmissionsBulkData(NpgSqlContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        LogHelper.info("Executing ApportionedEmissionsBulkDataFiles job");
        try
        {
          var sql_command = @"select rp.calendar_year as year, rp.quarter, p.state as state_cd, rp.begin_date, rp.end_date
                                from camdecmps.dm_emissions dme
                                join camdecmpsmd.reporting_period rp using (rpt_period_id)
                                join camdecmps.monitor_plan mp using (mon_plan_id)
                                join camd.plant p using (fac_id)
                                left join camdecmps.dm_emissions_user dmeu
                                    on dme.dm_emissions_id = dmeu.dm_emissions_id
                                    and dmeu.dm_emissions_user_cd = 'S3BDF'
                                where dmeu.dm_emissions_id is null
                                group by rp.calendar_year, rp.quarter, rp.begin_date, rp.end_date, p.state
                                order by year, quarter, state_cd;";

          List<List<Object>> rows = await _dbContext.ExecuteSqlQuery(sql_command, 5);

          string[] urls = {"/emissions-mgmt/apportioned/hourly/stream?", "/emissions-mgmt/apportioned/daily/stream?", "/emissions-mgmt/apportioned/mats/stream?", };
          string[] fileNames = {"/emissions/hourly/", "/emissions/daily/", "/emissions/mats/"};

          for(int row = 0; row < rows.Count; row++){
            for(int urlIndex = 0; urlIndex < urls.Length; urlIndex++){
              for(int copies = 0; copies < 1; copies++){
                Guid job_id = new Guid();
                IJobDetail newJob = BulkDataFile.CreateJobDetail();
                newJob.JobDataMap.Add(new KeyValuePair<string, object>("job_id", job_id));
                newJob.JobDataMap.Add(new KeyValuePair<string, object>("parent_job_id", parent_job_id));
                newJob.JobDataMap.Add(new KeyValuePair<string, object>("format", "csv"));

                if(copies == 0){
                  newJob.JobDataMap.Add(new KeyValuePair<string, object>("url", urls[urlIndex]+ "beginDate=" + rows[row][3] + "&endDate="+rows[row][4]));
                  newJob.JobDataMap.Add(new KeyValuePair<string, object>("fileName", fileNames[urlIndex] + "quarter/Emissions-Hourly-"+rows[row][0] + "-Q" + rows[row][1] +".csv"));
                }else{
                  newJob.JobDataMap.Add(new KeyValuePair<string, object>("url", urls[urlIndex] + "beginDate="+rows[row][0]+"-01-01&endDate="+rows[row][0]+"-12-31&stateCode=" + rows[row][2]));
                  newJob.JobDataMap.Add(new KeyValuePair<string, object>("fileName", fileNames[urlIndex] + "state/Emissions-Hourly-" + rows[row][0]+"-"+rows[row][2]+ ".csv"));
                }

                await context.Scheduler.ScheduleJob(newJob, TriggerBuilder.Create().StartNow().Build());
              }
            }
          }
        }
        catch (Exception e)
        {
          LogHelper.error(e.Message, new LogVariable("stack", e.StackTrace));
        }

        LogHelper.info("Executed ApportionedEmissionsBulkDataFiles job successfully");
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
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
      return JobBuilder.Create<ApportionedEmissionsBulkData>()
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