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
            
            string[] types = new string[]{"MP", "QA", "EM"};

            foreach(string type in types){
                _logger.LogInformation("Checking {Type} evaluations", type);
                
                List<Evaluation> inQueue = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'QUEUED'
                    ORDER BY queued_time", type
                ).ToList();

                _logger.LogInformation("Found {Count} {Type} evaluations in QUEUED status", 
                    inQueue.Count, type);

                List<Evaluation> wip = _dbContext.Evaluations.FromSqlRaw(@"
                    SELECT *
                    FROM camdecmpsaux.evaluation_queue
                    WHERE process_cd = {0} AND status_cd = 'WIP'
                    ORDER BY queued_time", type
                ).ToList();

                _logger.LogInformation("Found {Count} {Type} evaluations in WIP status", 
                    wip.Count, type);

                int maxAllowed = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_MAX_" + type +"_EVALUATIONS"]);
                _logger.LogInformation("Max allowed {Type} evaluations: {MaxAllowed}", 
                    type, maxAllowed);

                if(wip.Count < maxAllowed){
                    if(inQueue.Count > 0){
                        int jobs_to_schedule = maxAllowed - wip.Count;
                        _logger.LogInformation("Attempting to schedule {JobCount} {Type} evaluations", 
                            jobs_to_schedule, type);

                        for(int i = 0; i < jobs_to_schedule; i++){
                            if(i < inQueue.Count){
                                Evaluation toSchedule = inQueue[i];
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
                        }
                    }
                } else {
                    _logger.LogInformation("Maximum number of {Type} evaluations ({MaxAllowed}) already in progress", 
                        type, maxAllowed);
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
