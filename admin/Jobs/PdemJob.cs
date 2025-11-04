using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;

using DatabaseAccess;
using ECMPS.DM;
using ECMPS.Checks.EmissionsReport;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{

    [DisallowConcurrentExecution]
    public class PdemJob : IJob, IJobMetadata<PdemJob>
    {
        public static string JobName => "Program Data Emissions Job Queue";
        public static string JobDescription => "Operates on an interval to determine if emission reports in PDEM_REPORT table need to be generated.";
        public static string JobGroup => Constants.QuartzGroups.MAINTAINANCE;
        public static string TriggerName => "Program Data Emissions Job Queue Trigger";
        public static string TriggerDescription => "Operate every minute to determine if there are PDEM reports which can be triggered for generation";

        public PdemJob(NpgSqlContext dbContext, IConfiguration configuration, ILogger<cUpdateEmissionsDb> logger)
        {
            _dbContext = dbContext;
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }
        private NpgSqlContext _dbContext = null;
        private readonly ILogger<cUpdateEmissionsDb> _logger;

        public Task Execute(IJobExecutionContext context)
        {
            int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_PDEM"] ?? "3");

            try
            {
                for (int dex = 1; dex <= maxAllowed; dex++)
                {
                    List<PdemReport> pdemReportList = _dbContext.PdemReport.FromSql($"select pdem_report_id, mon_plan_id, rpt_period_id, submission_id from camdecmpsaux.PDEM_Job_Get_Next({maxAllowed})").ToList();

                    if (pdemReportList.Count > 0)
                    {
                        int commandTimeout = Configuration.GetValue<int>("EASEY_DB_COMMAND_TIMEOUT", 300);
                        cUpdateEmissions updateEmissions = new(ConnectionStringManager.getConnectionString(Configuration), _logger, commandTimeout);

                        updateEmissions.ProcessEmissionReport(pdemReportList[0].PdemReportId);
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in evaluation queue: {ErrorMessage}", ex.Message);
                return Task.FromException(ex);
            }
        }

    }

}
