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


namespace Epa.Camd.Quartz.Scheduler.Jobs
{

    public class PdemJob : IJob
    {

        public PdemJob(NpgSqlContext dbContext, IConfiguration configuration, ILogger<PdemJob> logger)
        {
            _dbContext = dbContext;
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }
        private NpgSqlContext _dbContext = null;
        private readonly ILogger<PdemJob> _logger;

        public Task Execute(IJobExecutionContext context)
        {
            string instanceIndex = Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX") ?? "unknown";
            int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_PDEM"] ?? "3");  //TODO: Need to create/handle EASEY_QUARTZ_SCHEDULER_MAX_PDEM

            try
            {
                for (int dex = 1; dex <= maxAllowed; dex++)
                {
                    List<PdemReport> pdemReportList = _dbContext.PdemReport.FromSql($"select pdem_report_id, mon_plan_id, rpt_period_id, submission_id from camdecmpsaux.PDEM_Job_Get_Next({maxAllowed})").ToList();

                    if (pdemReportList.Count > 0)
                    {
                        cUpdateEmissions updateEmissions = new(ConnectionStringManager.getConnectionString(Configuration), _logger, 20);

                        updateEmissions.ProcessEmissionReport(pdemReportList[0].PdemReportId);
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError("[Instance {InstanceIndex}] Error in evaluation queue: {ErrorMessage}", instanceIndex, ex.Message);
                return Task.FromException(ex);
            }
        }

    }

}
