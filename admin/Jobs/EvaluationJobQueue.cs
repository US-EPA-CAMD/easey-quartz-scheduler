using System;
using System.Threading.Tasks;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  [DisallowConcurrentExecution]
  public class EvaluationJobQueue : IJob
  {
    private NpgSqlContext _dbContext = null;
    private readonly ILogger<EvaluationJobQueue> _logger;
    private IConfiguration Configuration { get; }

    public EvaluationJobQueue(NpgSqlContext dbContext, IConfiguration configuration, ILogger<EvaluationJobQueue> logger)
    {
      _dbContext = dbContext;
      Configuration = configuration;
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting evaluation queue check");
            
            string[] processTypes = new string[]{"MP", "QA", "EM"};

            foreach(string processType in processTypes){
                _logger.LogInformation("Checking {Type} evaluations", processType);
                
                List<Evaluation> inQueue = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'QUEUED'
                    ORDER BY queued_time", processType
                ).ToList();

                _logger.LogInformation("Found {Count} {Type} evaluations in QUEUED status", 
                    inQueue.Count, processType);

                // Exit if nothing in queue
                if (inQueue.Count == 0) continue;

                List<Evaluation> wipOrClaimed = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd in ('WIP', 'CLAIMED')
                    ORDER BY queued_time", processType
                ).ToList();

                _logger.LogInformation("Found {Count} {Type} evaluations in WIP or CLAIMED status", 
                    wipOrClaimed.Count, processType);

                int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + processType +"_EVALUATIONS"]);
                _logger.LogInformation("Max allowed {Type} evaluations: {MaxAllowed}", 
                    processType, maxAllowed);

                // Exit if at max
                if(wipOrClaimed.Count >= maxAllowed)
                {
                  _logger.LogInformation("Maximum number of {Type} evaluations ({MaxAllowed}) already in progress", 
                      processType, maxAllowed);
                  continue;
                }

                int jobs_to_schedule = maxAllowed - wipOrClaimed.Count;
                _logger.LogInformation("Attempting to schedule {JobCount} {Type} evaluations", 
                    jobs_to_schedule, processType);

                // Set to CLAIMED
                List<Evaluation> claimed = _dbContext.Evaluations
                  .FromSqlRaw(@"
                      UPDATE camdecmpsaux.evaluation_queue
                      SET status_cd = 'CLAIMED'
                      WHERE evaluation_id IN (
                          SELECT evaluation_id
                          FROM camdecmpsaux.evaluation_queue
                          WHERE process_cd = {0}
                            AND status_cd = 'QUEUED'
                          ORDER BY queued_time
                          LIMIT {1}
                          FOR UPDATE SKIP LOCKED
                      )
                      RETURNING *;",
                      processType, jobs_to_schedule)
                  .ToList();

                _logger.LogInformation("Claimed {Count} {Type} evaluations for processing",
                    claimed.Count, processType);

                foreach(Evaluation toSchedule in claimed){
                    try
                    {
                        _logger.LogInformation("Processing evaluation ID {EvalId}",
                            toSchedule.EvaluationId);

                        EvaluationSet es = _dbContext.EvaluationSet.Find(toSchedule.EvaluationSetId);

                        _logger.LogInformation("Starting CheckEngineEvaluation for ID {EvalId}",
                            toSchedule.EvaluationId);

                        await CheckEngineEvaluation.StartNow(
                            context.Scheduler,
                            toSchedule.EvaluationId,
                            es.SetId,
                            toSchedule.ProcessCode,
                            es.FacId,
                            es.FacName,
                            es.MonPlanId,
                            es.Config,
                            es.UserId,
                            es.UserEmail,
                            toSchedule.QueuedTime,
                            toSchedule.TestSumId,
                            toSchedule.QaCertEventId,
                            toSchedule.TeeId,
                            toSchedule.RptPeriod
                        );

                        int delaySeconds = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_EVALUATION_JOB_QUEUE_DELAY"] ?? "1");
                        _logger.LogInformation("Waiting {Delay}s before next evaluation",
                            delaySeconds);
                        Thread.Sleep(delaySeconds * 1000);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Error starting CheckEngineEvaluation for evaluation ID {EvalId}: {ErrorMessage}",
                            toSchedule.EvaluationId, e.Message);
                        // Reset to QUEUED
                        toSchedule.StatusCode = "QUEUED";
                        _dbContext.Evaluations.Update(toSchedule);
                        _dbContext.SaveChanges();
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in evaluation queue: {ErrorMessage}",  e.Message);
            return;
        }
    }
  }
}
