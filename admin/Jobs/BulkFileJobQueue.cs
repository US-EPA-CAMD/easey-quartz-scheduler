using System;
using System.Threading.Tasks;

using Quartz;

using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkFileJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;

    private IConfiguration Configuration { get; }
    private readonly ILogger<BulkFileJobQueue> _logger;

    public BulkFileJobQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<BulkFileJobQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        Console.Write("Checking Queue Now");

        List<BulkFileQueue> inQueue = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd = 'QUEUED'"
          ).ToList();

        List<BulkFileQueue> inWIP = _dbContext.BulkFileQueue.FromSqlRaw(@"
            SELECT *
            FROM camdaux.bulk_file_queue
            WHERE status_cd = 'WIP'"
          ).ToList();


        if (inWIP.Count < Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"]))
        {
          if (inQueue.Count > 0)
          {
            int jobs_to_schedule = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_BULK_FILE_JOBS"]) - inWIP.Count;
            Console.WriteLine("Scheduling Jobs: " + jobs_to_schedule);
            int index = 0;
            for (int i = 0; i < jobs_to_schedule; i++)
            {
              if (index < inQueue.Count)
              {
                await BulkDataFile.CreateAndScheduleJobDetail(inQueue[i], _logger);
                Thread.Sleep(Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_BULK_FILE_JOB_QUEUE_DELAY"] ?? "5") * 1000);
                index++;
              }
            }
          }
        }

        return;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return;
      }
    }
  }
}
