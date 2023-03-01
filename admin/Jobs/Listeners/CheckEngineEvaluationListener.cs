using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Epa.Camd.Quartz.Scheduler.Models;
using Quartz;
using Quartz.Listener;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Epa.Camd.Quartz.Scheduler.Jobs.Listeners
{
  public class CheckEngineEvaluationListener : JobListenerSupport
  {
    public override string Name => "Check Engine Evaluation Listener";

    private IConfiguration Configuration { get; }
    private NpgSqlContext _dbContext = null;


    public CheckEngineEvaluationListener(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public override async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
    {
      JobDataMap dataMap = context.MergedJobDataMap;
      string setId = dataMap.GetString("SetId"); // Set id of the completed evaluation
      string userEmail = dataMap.GetString("UserEmail"); // Set id of the completed evaluation
      
      List<Evaluation> inSet = _dbContext.Evaluations.FromSqlRaw(@"
            SELECT *
            FROM camdecmpsaux.evaluation_queue
            WHERE evaluation_set_id = {0} AND status_cd not in ('COMPLETE', 'ERROR');", setId
          ).ToList();

      if(inSet.Count > 0){ // The jobs are not all done running yet, more eval records with this set id are coming up
        return;
      }
      
      /*
        Generate new client token for email request and send the email request -----------------
      */
      HttpClient client = new HttpClient();

      MassEvalEmail payload = new MassEvalEmail();
      payload.fromEmail = Configuration["EASEY_QUARTZ_SCHEDULER_MASS_EVALUATION_EMAIL"];
      payload.toEmail = userEmail;
      payload.evaluationSetId = setId;

      StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
      client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
      client.DefaultRequestHeaders.Add("x-client-id", Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"]);

      string clientToken = await Utils.generateClientToken();     
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

      HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_CAMD_SERVICES"] + "/support/email/mass-eval", httpContent);
      response.EnsureSuccessStatusCode();

      await base.JobWasExecuted(context, jobException, cancellationToken);
    }
  }
}