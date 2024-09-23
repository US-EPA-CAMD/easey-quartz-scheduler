using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
    public class InventoryChanges : IJob
    {
        private Guid job_id = Guid.NewGuid();

        private NpgSqlContext _dbContext = null;

        private IConfiguration Configuration { get; }

        public static class InventoryChangesIdentity
        {
            public static readonly string Group = Constants.QuartzGroups.MAINTAINANCE;
            public static readonly string JobName = "Inventory Changes";
            public static readonly string JobDescription = "Operates on an interval to determine if any remote facility/unit inventory changes require changes to existing monitoring plans.";
            public static readonly string TriggerName = "Check status log every 5 minutes";
            public static readonly string TriggerDescription = "Operate every 5 minutes to determine if there are any new changes recorded in the inventory status log.";
        }

        public static void RegisterWithQuartz(IServiceCollection services)
        {
            services.AddQuartzJob<InventoryChanges>(WithInventoryChangesKey(), InventoryChangesIdentity.JobDescription);
        }

        public static async Task ScheduleWithQuartz(IScheduler scheduler, IApplicationBuilder app)
        {
            try
            {
                JobKey jobKey = WithInventoryChangesKey();
                string cronExpression = Utils.Configuration["EASEY_QUARTZ_SCHEDULER_INVENTORY_CHANGES_SCHEDULE"] ?? "0 0/5 * * * ?";
                TriggerBuilder triggerBuilder = WithInventoryChangesCronSchedule(cronExpression);

                if (await scheduler.CheckExists(jobKey))
                {
                    ITrigger trigger = await scheduler.GetTrigger(WithInventoryChangesTriggerKey());

                    if (
                      trigger is ICronTrigger cronTrigger &&
                      cronTrigger.CronExpressionString != cronExpression
                    )
                    {
                        await scheduler.RescheduleJob(WithInventoryChangesTriggerKey(), triggerBuilder.Build());
                        Console.WriteLine($"Rescheduled {jobKey.Name} with cron expression [{cronExpression}]");
                    }
                }
                else
                {
                    app.UseQuartzJob<InventoryChanges>(triggerBuilder);
                    Console.WriteLine($"Scheduled {jobKey.Name} with cron expression [{cronExpression}]");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(e.Message);
            }
        }

        public InventoryChanges(NpgSqlContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            Configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.Write("Checking Status Log Now");

            try
            {
                // Get the last job log record for the job name "Inventory Changes".
                JobLog lastJobLog = _dbContext.JobLogs.FromSqlRaw($@"
                        SELECT * FROM camdaux.job_log
                        WHERE job_name = '{jl.JobName}'
                        ORDER BY start_date DESC
                        LIMIT 1"
                        ).FirstOrDefault();

                // Check the status of the last job log. If the status is "WIP", then return.
                if (lastJobLog != null && lastJobLog.StatusCd == "WIP")
                {
                    return;
                }

                JobLog jl = new JobLog();
                try
                {
                    jl.JobId = job_id;
                    jl.JobSystem = "Quartz";
                    jl.JobClass = "Maintainance";
                    jl.JobName = "Inventory Changes";
                    jl.AddDate = Utils.getCurrentEasternTime();
                    jl.StartDate = Utils.getCurrentEasternTime();
                    jl.EndDate = null;
                    jl.StatusCd = "WIP";
                    _dbContext.JobLogs.Add(jl);
                    await _dbContext.SaveChangesAsync();

                    // Get the last processed inventory status log ID from the additional details of the last job log.
                    int lastProcessedInventoryStatusLogId = 0;
                    if (lastJobLog != null && lastJobLog.AdditionalDetails != null)
                    {
                        lastProcessedInventoryStatusLogId = JsonConvert.DeserializeObject<InventoryChangesJobLogAdditionalDetails>(lastJobLog.AdditionalDetails).LastProcessedInventoryStatusLogId;
                    }

                    // Retrieve all inventory status log records after the last processed log and with a data type code of eith `INVENTORY` or `UNIT_PROGRAM`.
                    List<InventoryStatusLog> inventoryStatusLogs = _dbContext.InventoryStatusLogs.FromSqlRaw($@"
                        SELECT * FROM camdaux.inventory_status_log
                        WHERE inventory_status_log_id > {lastProcessedInventoryStatusLogId}
                        AND data_type_cd IN ('INVENTORY', 'UNIT_PROGRAM')
                        ORDER BY inventory_status_log_id"
                        ).ToList();

                    // TODO: Call the stored procedure `camdecmpswks.update_mp_eval_status_and_reporting_freq` for each log record.

                    // TODO: Update the additional details table with the ID of the last processed inventory status log.

                    jl.StatusCd = "COMPLETE";
                    jl.EndDate = Utils.getCurrentEasternTime();
                    _dbContext.JobLogs.Update(jl);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    jl.StatusCd = "ERROR";
                    jl.EndDate = Utils.getCurrentEasternTime();
                    jl.AdditionalDetails = e.Message;
                    _dbContext.JobLogs.Update(jl);
                    await _dbContext.SaveChangesAsync();
                    Console.Write(e.Message);
                }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }

        public static JobKey WithInventoryChangesKey()
        {
            return new JobKey(InventoryChangesIdentity.JobName, InventoryChangesIdentity.Group);
        }

        public static TriggerKey WithInventoryChangesTriggerKey()
        {
            return new TriggerKey(InventoryChangesIdentity.TriggerName, InventoryChangesIdentity.Group);
        }

        public static IJobDetail WithInventoryChangesJobDetail()
        {
            return JobBuilder.Create<InventoryChanges>()
                .WithIdentity(WithInventoryChangesKey())
                .WithDescription(InventoryChangesIdentity.JobDescription)
                .Build();
        }

        public static TriggerBuilder WithInventoryChangesCronSchedule(string cronExpression)
        {
            return TriggerBuilder.Create()
                .WithIdentity(WithInventoryChangesTriggerKey())
                .WithDescription(InventoryChangesIdentity.TriggerDescription)
                .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression).InTimeZone(Utils.getCurrentEasternZone()));
        }
    }
}
