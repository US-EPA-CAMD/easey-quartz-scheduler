using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;
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

    private async Task<string[]> getProgramCodeList(string[] programCodes){
      string[] programCodesToReturn;
      if(programCodes.Length == 0 || programCodes[0] == "*"){
        List<List<Object>> programCodeRows = await this.dbContext.ExecuteSqlQuery("SELECT prg_cd FROM camdmd.program_code", 1);
        programCodesToReturn = new string[programCodeRows.Count];

        for(int i = 0; i < programCodeRows.Count; i++){
          programCodesToReturn[i] = (string) programCodeRows[i][0];
        }
      }else{
        programCodesToReturn = programCodes;
      }

      return programCodesToReturn;
    }

    private async Task generateMassFacilityJobs(int? from, int? to){
      for(int? year = from; year <= to; year++){
          DateTime currentDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
          await this.dbContext.CreateBulkFileJob(year, null, null, "Facility", null, Configuration["EASEY_STREAMING_SERVICES"] + "/facilities/attributes?year=" + year, "facility/facility" + "-" + year + ".csv", job_id, null);
      }
    }

    private async Task generateMassEmissionsCompliance(){
      await this.dbContext.CreateBulkFileJob(null, null, null, "Compliance", null, Configuration["EASEY_STREAMING_SERVICES"] + "/emissions-compliance", "compliance/emissions-compliance-arpnox.csv", job_id, "ARP");
    }

    private async Task generateMassAllowanceComplianceJobs(string[] programCodes){
      string[] codes = await getProgramCodeList(programCodes);
      foreach (string code in codes)
      {
        string urlParams = "programCodeInfo=" + code;
        await this.dbContext.CreateBulkFileJob(null, null, null, "Compliance", null, Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-compliance?" + urlParams, "compliance/compliance-" + code.ToLower() + ".csv", job_id, code);
        
      }  
    }

    private async Task generateMassAllowanceTransactionsJobs(int? from, int? to, string[] programCodes){
      string[] codes = await getProgramCodeList(programCodes);
      for(int? year = from; year <= to; year++){
        foreach (string code in codes)
        {
          string urlParams = "transactionBeginDate=" + year + "-01-01&transactionEndDate=" + year + "-12-31&programCodeInfo=" + code;
          await this.dbContext.CreateBulkFileJob(year, null, null, "Allowance", null, Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-transactions?" + urlParams, "allowance/transactions-" + code.ToLower() + ".csv", job_id, code);
        }  
      }
    }

    private async Task generateMassEmissionsForStates(int? from, int? to, string[] stateCodes, string[] dataSubTypes){
      string[] stateCodesToIterate;

      if(stateCodes.Length == 0 || stateCodes[0] == "*"){
        List<List<Object>> rowsPerState = await this.dbContext.ExecuteSqlQuery("SELECT * FROM camdmd.state_code", 5);
        stateCodesToIterate = new string[rowsPerState.Count];

        for(int i = 0; i < rowsPerState.Count; i++){
          stateCodesToIterate[i] = (string) rowsPerState[i][0];
        }
      }else{
        stateCodesToIterate = stateCodes;
      }
      
      for(int? year = from; year <= to; year++){
        foreach (string stateCd in stateCodesToIterate)
        {
          string urlParams = "beginDate=" + year + "-01-01&endDate=" + year + "-12-31&stateCode=" + stateCd;

          foreach(string dataSubType in dataSubTypes){
            await this.dbContext.CreateBulkFileJob(year, null, stateCd, "Emissions", dataSubType, Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/" + dataSubType.ToLower() + "?" + urlParams, "emissions/" + dataSubType.ToLower() + "/state/emissions-"+ dataSubType.ToLower() +"-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
          }
        }  
      }
    }

  private async Task generateMassEmissionsForQuarters(int? from, int? to, int[] quarters, string[] dataSubTypes){
      int[] quartersToIterate;

      if(quarters.Length == 0){
        quartersToIterate = new int[]{1, 2, 3, 4};
      }else{
        quartersToIterate = quarters;
      }
      
      for(int? year = from; year <= to; year++){
        foreach (int quarter in quartersToIterate)
        {

          string beginDate;
          string endDate;

          if(quarter == 1){
            beginDate = year + "-01-01";
            endDate = year + "-03-31";
          }
          else if(quarter == 2){
            beginDate = year + "-04-01";
            endDate = year + "-06-30";
          }
          else if(quarter == 3){
            beginDate = year + "-07-01";
            endDate = year + "-09-30"; 
          }else{
            beginDate = year + "-10-01";
            endDate = year + "-12-31"; 
          }

          string urlParams = "beginDate=" + beginDate + "&endDate=" + endDate;
          foreach(string dataSubType in dataSubTypes){
            await this.dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", dataSubType, Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/" + dataSubType.ToLower() + "?" + urlParams, "emissions/" + dataSubType.ToLower() + "/quarter/emissions-" + dataSubType.ToLower() + "-" + year + "-q" + quarter + ".csv", job_id, null);
          }
        }  
      }
    }

    [HttpPost("bulk-file")]
    public async Task<ActionResult> CreateMassBulkFileApi([FromBody] OnDemandBulkFileRequest massRequest)
    {
      
      /*
      string errorMessage = await Utils.validateRequestCredentialsClientToken(Request, Configuration);
      if(errorMessage != "")
      {
        return BadRequest(errorMessage);
      }
      */
      
      JobLog jl = new JobLog();
      jl.JobId = job_id;
      jl.JobSystem = "Quartz";
      jl.JobClass = "Bulk Data File";
      jl.JobName = "Mass Bulk File Generation API";
      jl.AddDate = Utils.getCurrentEasternTime();
      jl.StartDate = Utils.getCurrentEasternTime();
      jl.EndDate = null;
      jl.StatusCd = "WIP";

      dbContext.JobLogs.Add(jl);
      await dbContext.SaveChangesAsync();

      try
      {

        if(massRequest.emissionsCompliance){
          await generateMassEmissionsCompliance();
        }

        if(massRequest.ProgramCodes != null){
          if(massRequest.To == null || massRequest.From == null){
            Console.WriteLine("Generating Mass Allowance Compliance Data");
            await generateMassAllowanceComplianceJobs(massRequest.ProgramCodes);
          }
          else if(massRequest.To != null && massRequest.From != null){
            Console.WriteLine("Generating Mass Transactions Compliance Data");
            await generateMassAllowanceTransactionsJobs(massRequest.From, massRequest.To, massRequest.ProgramCodes);
          }
        }
        else if(massRequest.To != null && massRequest.From != null){
          if(massRequest.SubTypes != null){
            if(massRequest.StateCodes != null)
            {
              Console.WriteLine("Generating Mass Emissions State Data");
              await generateMassEmissionsForStates(massRequest.From, massRequest.To, massRequest.StateCodes, massRequest.SubTypes);        
            }

            if(massRequest.Quarters != null)
            {
              Console.WriteLine("Generating Mass Emissions Quarterly Data");
              await generateMassEmissionsForQuarters(massRequest.From, massRequest.To, massRequest.Quarters, massRequest.SubTypes);        
            }
          }else{
            Console.WriteLine("Generating Mass Facility Data");
            await generateMassFacilityJobs(massRequest.From, massRequest.To);
          }
        }   

        jl.StatusCd = "COMPLETE";
        jl.EndDate = Utils.getCurrentEasternTime();
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