using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.Net;

using Amazon;
using Amazon.S3;
using Epa.Camd.Quartz.Scheduler.Models;

using Quartz;
using SilkierQuartz;

using Epa.Camd.Logger;

namespace Epa.Camd.Quartz.Scheduler.Jobs
{
  public class BulkDataFile : IJob
  {

    public IConfiguration Configuration { get; }

    private NpgSqlContext _dbContext = null;

    public static class Identity
    {
      public static readonly string Group = Constants.QuartzGroups.BULK_DATA;
      public static readonly string JobName = "Bulk Data File";
      public static readonly string JobDescription = "Streams a file to an S3 bucket";
    }
    
    public static void RegisterWithQuartz(IServiceCollection services)
    {
      services.AddQuartzJob<BulkDataFile>(WithJobKey(), Identity.JobDescription);
    }
    
    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

    public BulkDataFile(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    private async Task<string> getDescription(string dataType, string subType, decimal? year, decimal? quarter, string state, string programCode){
      string description = "";

      List<ProgramCode> programs = await _dbContext.getProgramCodes();

      if (dataType.Equals("facilities", StringComparison.OrdinalIgnoreCase))
        {
          description = $"Facility/Unit attributes data for {year}";
        }
        else if (
          dataType.Equals("mats", StringComparison.OrdinalIgnoreCase) ||
          dataType.Equals("emissions", StringComparison.OrdinalIgnoreCase)
        )
        {
          string regulation = dataType.Equals("mats", StringComparison.OrdinalIgnoreCase)
            ? "Mercury and Air Toxics Standards (MATS)"
            : "Part 75";

          if (state != null){
            description = $"Unit-level {subType} {regulation} emissions data for all {state.ToUpper()} facilities/units for {year}";
          }

          if (quarter != null){
            description = $"Unit-level {subType} {regulation} emissions data for all facilities/units for {year}  Q{quarter}";
          }
        }
        else if (dataType.Equals("allowance", StringComparison.OrdinalIgnoreCase))
        {
            description = $"{programs.Find(i => i.Code == programCode.ToUpper()).Description} Allowance Transactions Data";
        }
        else if (dataType.Equals("compliance", StringComparison.OrdinalIgnoreCase))
        {
          description = $"{programs.Find(i => i.Code == programCode.ToUpper()).Description} Annual Reconciliation Data";
        }
      return description;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      string url =  (string) context.JobDetail.JobDataMap.Get("url");
      string fileName = (string) context.JobDetail.JobDataMap.Get("fileName");
      Guid job_id = (Guid) context.JobDetail.JobDataMap.Get("job_id");

      string stateCode = (string) context.JobDetail.JobDataMap.Get("StateCode");
      string dataType = (string) context.JobDetail.JobDataMap.Get("DataType");
      string dataSubType = (string) context.JobDetail.JobDataMap.Get("DataSubType");
      decimal? quarter = (decimal) context.JobDetail.JobDataMap.Get("Quarter");
      decimal? year = (decimal) context.JobDetail.JobDataMap.Get("Year");
      string programCode = (string) context.JobDetail.JobDataMap.Get("ProgramCode");

      string description = await getDescription(dataType, dataSubType, year, quarter, stateCode, programCode);

      LogHelper.info("Executing new stream", new LogVariable("url", url));

      IAmazonS3 s3Client = new AmazonS3Client(
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_ACCESS_KEY_ID"],
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_SECRET_ACCESS_KEY"],
        RegionEndpoint.USGovCloudWest1
      );

      JobLog bulkFile = await _dbContext.JobLogs.FindAsync(job_id);

      try
      {        
        bulkFile.StatusCd = "WIP";
        bulkFile.StartDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        _dbContext.JobLogs.Update(bulkFile);
        await _dbContext.SaveChangesAsync();

        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        myHttpWebRequest.Headers.Add("x-api-key", Configuration["QUARTZ_API_KEY"]);
        myHttpWebRequest.Headers.Add("accept", (string) context.JobDetail.JobDataMap.Get("format"));
        myHttpWebRequest.Timeout = 900000;

        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

        Stream s = myHttpWebResponse.GetResponseStream();
        
        List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
        
        InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
            Key = fileName,
        };

        initiateRequest.Metadata.Add("Description", description);

        if(year != null)
          initiateRequest.Metadata.Add("Year", year.ToString());

        if(stateCode != null)
          initiateRequest.Metadata.Add("StateCode", stateCode);
        
        if(dataType != null)
          initiateRequest.Metadata.Add("DataType", dataType);

        if(dataSubType != null)
          initiateRequest.Metadata.Add("DataSubType", dataSubType);

        if(quarter != null)
          initiateRequest.Metadata.Add("Quarter", quarter.ToString());

        if(programCode != null)
          initiateRequest.Metadata.Add("ProgramCode", programCode);

        InitiateMultipartUploadResponse initResponse = await s3Client.InitiateMultipartUploadAsync(initiateRequest);

        const int bufferSize = 5242880;
        var bytes = new byte[bufferSize];

        int readBytes;
        int totalReadBytes;
        int uploadPartNumber = 1;

        while(true)
        {
            totalReadBytes = 0;
            do{
               readBytes = await s.ReadAsync(
                bytes,
                totalReadBytes,
                bufferSize - totalReadBytes);

                totalReadBytes += readBytes;

                if (readBytes == 0)
                    break;  
                Console.WriteLine(readBytes);
            }while(totalReadBytes < bufferSize);

            if(totalReadBytes == 0){
              break;
            }

            UploadPartRequest uploadRequest = new UploadPartRequest
            {
                BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
                Key = fileName,
                UploadId = initResponse.UploadId,
                PartNumber = uploadPartNumber,
                PartSize = bufferSize,
                InputStream = new MemoryStream(bytes)
            };

            uploadResponses.Add(await s3Client.UploadPartAsync(uploadRequest));

            bytes = new byte[bufferSize];
            await s.FlushAsync();

            uploadPartNumber++;
        }

        myHttpWebResponse.Close();
        //myHttpWebRequest.Abort();

        if(uploadPartNumber != 1 || totalReadBytes > 0){

          CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
          {
              BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
              Key = fileName,
              UploadId = initResponse.UploadId
          };
          completeRequest.AddPartETags(uploadResponses);

            // Complete the upload.
          CompleteMultipartUploadResponse completeUploadResponse =
              await s3Client.CompleteMultipartUploadAsync(completeRequest);

        }


        bulkFile.StatusCd = "COMPLETE";
        bulkFile.EndDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        _dbContext.JobLogs.Update(bulkFile);
        _dbContext.SaveChanges();

        LogHelper.info("Executed stream successfully", new LogVariable("url", url));      
      }
      catch (Exception e)
      {
        Console.Write(e);
        LogHelper.error(e.Message);
        bulkFile.StatusCd = "ERROR";
        bulkFile.EndDate = TimeZoneInfo.ConvertTime (DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        bulkFile.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(bulkFile);
        _dbContext.SaveChanges();
      }
    }

    public static IJobDetail CreateJobDetail(string key)
    {
      return JobBuilder.Create<BulkDataFile>().WithIdentity(new JobKey(key, Constants.QuartzGroups.BULK_DATA)).StoreDurably().Build();
    }

  }
}