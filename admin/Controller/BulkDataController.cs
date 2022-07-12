using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Epa.Camd.Quartz.Scheduler.Models;
using Newtonsoft.Json;
using Epa.Camd.Quartz.Scheduler.Jobs;

namespace Epa.Camd.Quartz.Scheduler
{
    [ApiController]
  [Produces("application/json")]
  [Route("quartz-mgmt/bulk-files")]
  public class BulkDataController : ControllerBase
  {
    private AmazonS3Client client;
    private NpgSqlContext dbContext = null;
    private List<ProgramCode> programs = null;
    private IConfiguration Configuration { get; }

    private Guid job_id = Guid.NewGuid();

    public BulkDataController(
      NpgSqlContext dbContext,
      IConfiguration configuration
    )
    {
      this.dbContext = dbContext;
      Configuration = configuration;
      client = new AmazonS3Client(
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_ACCESS_KEY_ID"],
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_SECRET_ACCESS_KEY"],
        RegionEndpoint.USGovCloudWest1
      );
    }

    [HttpPost("create-bulk-file")]
    
    public async Task<ActionResult> CreateBulkFileApi([FromBody] string jsonList)
    {
      string errorMessage = await Utils.validateRequestCredentialsClientToken(Request, Configuration);
      if(errorMessage != "")
      {
        return BadRequest(errorMessage);
      }

      Console.Write("Made it here");

      JobLog jl = new JobLog();
      jl.JobId = job_id;
      jl.JobSystem = "Quartz";
      jl.JobClass = "Bulk Data File";
      jl.JobName = "Bulk File Generation API";
      jl.AddDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
      jl.StartDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
      jl.EndDate = null;
      jl.StatusCd = "WIP";

      dbContext.JobLogs.Add(jl);
      await dbContext.SaveChangesAsync();

      try
      {
        List<BulkFileParameters> objects = JsonConvert.DeserializeObject<List<BulkFileParameters>>(jsonList);

        for(int i = 0; i < objects.Count; i++){
          BulkFileJobQueue.AddBulkDataJobToQueue(await dbContext.CreateBulkFileJob(objects[i].Year, objects[i].Quarter, objects[i].StateCode, objects[i].DataType, objects[i].SubType + "- On Demand", objects[i].Url, objects[i].FileName, job_id, objects[i].ProgramCode));
        }      

        jl.StatusCd = "COMPLETE";
        jl.EndDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        dbContext.JobLogs.Update(jl);
        await dbContext.SaveChangesAsync();  
      }
      catch (Exception e)
      {
        Console.WriteLine("Exception: " + e.ToString());
        Console.ReadKey();
        return new BadRequestResult();
      }

      return new OkResult();
    }

  }
}