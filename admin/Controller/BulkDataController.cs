using System.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler
{


  class QuarterDates
  {
      public string beginDate {get; set;}
      public string endDate {get; set; }
  }


  [ApiController]
  [Produces("application/json")]
  [Route("quartz-mgmt/bulk-files")]
  public class BulkDataController : ControllerBase
  {
    private AmazonS3Client client;
    private NpgSqlContext dbContext = null;
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

    private QuarterDates getQuarterDates(int quarter, int? year){
      QuarterDates qd = new QuarterDates();

      if(quarter == 1){
        qd.beginDate = year + "-01-01";
        qd.endDate = year + "-03-31";
      }
      else if(quarter == 2){
        qd.beginDate = year + "-04-01";
        qd.endDate = year + "-06-30";
      }
      else if(quarter == 3){
        qd.beginDate = year + "-07-01";
        qd.endDate = year + "-09-30"; 
      }else{
        qd.beginDate = year + "-10-01";
        qd.endDate = year + "-12-31"; 
      }

      return qd;
    }

    private async Task<string[]> getProgramCodeList(string[] programCodes){
      string[] programCodesToReturn;
      if(programCodes.Length == 0 || programCodes[0] == "*"){
        List<List<Object>> programCodeRows = await this.dbContext.ExecuteSqlQuery("SELECT prg_cd FROM camdmd.program_code pc WHERE pc.active = 1", 1);
        programCodesToReturn = new string[programCodeRows.Count];

        for(int i = 0; i < programCodeRows.Count; i++){
          programCodesToReturn[i] = (string) programCodeRows[i][0];
        }
      }else{
        programCodesToReturn = programCodes;
      }

      return programCodesToReturn;
    }

    private async Task<string[]> getStateCodeList(string[] stateCodes){
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

      return stateCodesToIterate;
    }

    private async Task<int[]> getQuartersList(int[] quarters){
      int[] quartersToIterate;

      if(quarters.Length == 0){
        quartersToIterate = new int[]{1, 2, 3, 4};
      }else{
        quartersToIterate = quarters;
      }

      return quartersToIterate;
    }

    private async Task generateMassFacilityJobs(int? from, int? to){
      for(int? year = from; year <= to; year++){
          DateTime currentDate = Utils.getCurrentEasternTime();
          await this.dbContext.CreateBulkFileJob(year, null, null, "Facility", null, Configuration["EASEY_STREAMING_SERVICES"] + "/facilities/attributes?year=" + year, "facility/facility" + "-" + year + ".csv", job_id, null);
      }
    }

    private async Task generateMassEmissionsCompliance(){
      await this.dbContext.CreateBulkFileJob(null, null, null, "Compliance", null, Configuration["EASEY_STREAMING_SERVICES"] + "/emissions-compliance", "compliance/emissions-compliance-arpnox.csv", job_id, "ARP");
    }

    private async Task generateMassAllowanceHoldingsJobs(string[] programCodes){
      string[] codes = await getProgramCodeList(programCodes);
      foreach (string code in codes)
      {
          decimal year = DateTime.Now.ToUniversalTime().Year - 1;
          string urlParams = "programCodeInfo=" + code;

          await dbContext.CreateBulkFileJob(null, null, null, "Allowance", null, Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/allowance-holdings?" + urlParams, "allowance/holdings-" + code.ToLower() + ".csv", job_id, code);
      }  
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

    private async Task generateMassMATsForStates(int? from, int? to, string[] stateCodes){
      string[] stateCodesToIterate = await getStateCodeList(stateCodes);
      for(int? year = from; year <= to; year++){
        if(year >= 2015){ //MATs files only active after 2015
          foreach (string stateCd in stateCodesToIterate)
          {
              string urlParams = "beginDate=" + year + "-01-01&endDate=" + year + "-12-31&stateCode=" + stateCd;
              await this.dbContext.CreateBulkFileJob(year, null, stateCd, "Mercury and Air Toxics Emissions (MATS)", "Daily", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/state/mats-hourly-" + year + "-" + stateCd.ToLower() + ".csv", job_id, null);
          }  
        }
      }
    }

    private async Task generateMassMATsForQuarters(int? from, int? to, int[] quarters){
      int[] quartersToIterate = await getQuartersList(quarters);
      
      for(int? year = from; year <= to; year++){
        if(year >= 2015){ //MATs files only active after 2015
          foreach (int quarter in quartersToIterate)
          {
            QuarterDates qd = getQuarterDates(quarter, year);

            string urlParams = "beginDate=" + qd.beginDate + "&endDate=" + qd.endDate;
            await this.dbContext.CreateBulkFileJob(year, quarter, null, "Mercury and Air Toxics Emissions (MATS)", "Hourly", Utils.Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/mats/hourly?" + urlParams, "mats/hourly/quarter/mats-hourly-" + year + "-q" + quarter + ".csv", job_id, null);
          }
        }  
      }
    }

    private async Task generateMassEmissionsForStates(int? from, int? to, string[] stateCodes, string[] dataSubTypes){
      string[] stateCodesToIterate = await getStateCodeList(stateCodes);
      
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
      int[] quartersToIterate = await getQuartersList(quarters);
      
      for(int? year = from; year <= to; year++){
        foreach (int quarter in quartersToIterate)
        {
          QuarterDates qd = getQuarterDates(quarter, year);

          string urlParams = "beginDate=" + qd.beginDate + "&endDate=" + qd.endDate;
          foreach(string dataSubType in dataSubTypes){
            await this.dbContext.CreateBulkFileJob(year, quarter, null, "Emissions", dataSubType, Configuration["EASEY_STREAMING_SERVICES"] + "/emissions/apportioned/" + dataSubType.ToLower() + "?" + urlParams, "emissions/" + dataSubType.ToLower() + "/quarter/emissions-" + dataSubType.ToLower() + "-" + year + "-q" + quarter + ".csv", job_id, null);
          }
        }  
      }
    }

    [HttpPost("bulk-file")]
    public async Task<ActionResult> CreateMassBulkFileApi([FromBody] OnDemandBulkFileRequest massRequest)
    {
      string errorMsg;
      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"])){
        errorMsg = Utils.validateRequestCredentialsGatewayToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }
      if(Boolean.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_CLIENT_TOKEN"])){
        errorMsg = await Utils.validateRequestCredentialsClientToken(Request, Configuration);
        if(errorMsg != ""){
          return BadRequest(errorMsg);
        }
      }

      JobLog jl = new JobLog();
      jl.JobId = job_id;
      jl.JobSystem = "Quartz";
      jl.JobClass = "Bulk Data File";
      jl.JobName = "Mass Bulk File Generation API";
      jl.AddDate = Utils.getCurrentEasternTime();
      jl.StartDate = Utils.getCurrentEasternTime();
      jl.EndDate = null;
      jl.StatusCd = "WIP";
//
      dbContext.JobLogs.Add(jl);
      await dbContext.SaveChangesAsync();

      try
      {

        if(massRequest.generateStateMATS){
          await generateMassMATsForStates(massRequest.From, massRequest.To, massRequest.StateCodes);
        }

        if(massRequest.generateQuarterMATS){
          await generateMassMATsForQuarters(massRequest.From, massRequest.To, massRequest.Quarters);
        }

        if(massRequest.emissionsCompliance){
          await generateMassEmissionsCompliance();
        }

        if(massRequest.allowanceHoldings){
          await generateMassAllowanceHoldingsJobs(massRequest.ProgramCodes);
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