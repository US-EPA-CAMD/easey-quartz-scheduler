using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

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

    public static IScheduler BulkDataScheduler {get; set;}

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

      if (dataType.Equals("facility", StringComparison.OrdinalIgnoreCase))
        {
          description = $"Facility/Unit attributes data for {year}";
        }
        else if (
          dataType.Equals("mercury and air toxics emissions (mats)", StringComparison.OrdinalIgnoreCase) ||
          dataType.Equals("emissions", StringComparison.OrdinalIgnoreCase)
        )
        {
          string regulation = dataType.Equals("Mercury and Air Toxics Emissions (MATS)", StringComparison.OrdinalIgnoreCase)
            ? "Mercury and Air Toxics Standards (MATS)"
            : "Part 75";

          if (state != null){
            description = $"Unit-level {subType} {regulation} emissions data for all {state.ToUpper()} facilities/units for {year}";
          }

          if (quarter != null){
            description = $"Unit-level {subType} {regulation} emissions data for all facilities/units for {year} quarter {quarter}";
          }
        }
        else if (dataType.Equals("allowance", StringComparison.OrdinalIgnoreCase))
        {
            if(year != null){
              description = $"{programs.Find(i => i.Code == programCode.ToUpper()).Description} Allowance Transactions Data";
            }
            else{
              description = $"{programs.Find(i => i.Code == programCode.ToUpper()).Description} Allowance Holdings Data";
            }
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
        int? year = (int?) context.JobDetail.JobDataMap.Get("Year");
        int? quarter = (int?) context.JobDetail.JobDataMap.Get("Quarter");
        string programCode = (string) context.JobDetail.JobDataMap.Get("ProgramCode");
        BulkFileQueue bulkFile = await _dbContext.BulkFileQueue.FindAsync(job_id);
      try
      {       
        

      string description = await getDescription(dataType, dataSubType, year, quarter, stateCode, programCode);

      LogHelper.info("Executing new stream", new LogVariable("url", url));

      IAmazonS3 s3Client = new AmazonS3Client(
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_ACCESS_KEY_ID"],
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_SECRET_ACCESS_KEY"],
        RegionEndpoint.USGovCloudWest1
      );
        bulkFile.StatusCd = "WIP";

        bulkFile.StartDate = Utils.getCurrentEasternTime();

        _dbContext.BulkFileQueue.Update(bulkFile);
        await _dbContext.SaveChangesAsync();

        Console.Write(url);

        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        myHttpWebRequest.Headers.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        myHttpWebRequest.Headers.Add("accept", (string) context.JobDetail.JobDataMap.Get("format"));
        myHttpWebRequest.Timeout = 1803000;

        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

        Stream s = myHttpWebResponse.GetResponseStream();
        
        List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
        
        InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
            Key = fileName,
        };

        Dictionary<String, Object> Metadata = new Dictionary<string, object>();

        Metadata.Add("description", description);
        if(year != null)
          Metadata.Add("year", year.ToString());
        if(stateCode != null)
          Metadata.Add("stateCode", stateCode);
        if(dataType != null)
          Metadata.Add("dataType", dataType);
        if(dataSubType != null)
          Metadata.Add("dataSubType", dataSubType);
        if(quarter != null)
          Metadata.Add("quarter", quarter.ToString());
        if(programCode != null)
          Metadata.Add("programCode", programCode);

        InitiateMultipartUploadResponse initResponse = await s3Client.InitiateMultipartUploadAsync(initiateRequest);

        int bufferSize = Int32.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_BULK_BUFFER_SIZE"]);

        byte[] bytes = new byte[bufferSize];

        int readBytes;
        int totalReadBytes;
        int uploadPartNumber = 1;
        long totalWrittenBytes = 0;

        List<Task> parts = new List<Task>();

        while(true)
        {
            totalReadBytes = 0;
            do{
               readBytes = await s.ReadAsync(
                bytes,
                totalReadBytes,
                bufferSize - totalReadBytes);

                totalReadBytes += readBytes;
                totalWrittenBytes += readBytes;

                if (readBytes == 0)
                    break;  
            }while(totalReadBytes < bufferSize);

            if(totalReadBytes == 0){
              break;
            }

            if(totalReadBytes < bufferSize){ //Concatenate last chunk of data into min size
              byte[] copyBytes = new byte[totalReadBytes];
              for(int i = 0; i < totalReadBytes; i++){
                copyBytes[i] = bytes[i];
              }
              bytes = copyBytes;
            }

            UploadPartRequest uploadRequest = new UploadPartRequest
            {
              BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
              Key = fileName,
              UploadId = initResponse.UploadId,
              PartNumber = uploadPartNumber,
              PartSize = bufferSize,
              InputStream = new MemoryStream(bytes),
            };

            uploadResponses.Add(await s3Client.UploadPartAsync(uploadRequest));
            
            bytes = new byte[bufferSize];
            await s.FlushAsync();

            uploadPartNumber++;
        }

        String[] split = fileName.Split("/");
        string name = split[split.Length - 1];
        
        myHttpWebResponse.Close();
        
        if(uploadPartNumber != 1 || totalReadBytes > 0){

          BulkFileMetadata found = _dbContext.BulkFileMetadataSet.Find(name);
          if(found == null){
            BulkFileMetadata newMeta = new BulkFileMetadata();
            newMeta.FileName = name;
            newMeta.S3Path = fileName;
            newMeta.Metadata = JsonConvert.SerializeObject(Metadata);
            newMeta.FileSize = totalWrittenBytes;
            newMeta.AddDate = Utils.getCurrentEasternTime();
            newMeta.UpdateDate = Utils.getCurrentEasternTime();
            _dbContext.BulkFileMetadataSet.Add(newMeta);
            await _dbContext.SaveChangesAsync();
          }else{
            found.UpdateDate = Utils.getCurrentEasternTime();
            found.Metadata = JsonConvert.SerializeObject(Metadata);
            found.FileSize = totalWrittenBytes;
            _dbContext.BulkFileMetadataSet.Update(found);
            await _dbContext.SaveChangesAsync();
          }

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
        bulkFile.EndDate = Utils.getCurrentEasternTime();
        _dbContext.BulkFileQueue.Update(bulkFile);
        await _dbContext.SaveChangesAsync();

        await context.Scheduler.DeleteJob(new JobKey(job_id.ToString()));

        LogHelper.info("Executed stream successfully", new LogVariable("url", url));      
      }
      catch (Exception e)
      {
        Console.WriteLine("ERRORED");
        Console.WriteLine(bulkFile.JobId);
        Console.Write(e);
        LogHelper.error(e.Message);
        
        try{
        bulkFile.StatusCd = "ERROR";
        bulkFile.EndDate = Utils.getCurrentEasternTime();

        bulkFile.AdditionalDetails = e.ToString();
        _dbContext.BulkFileQueue.Update(bulkFile);
        await _dbContext.SaveChangesAsync();
        }
        catch(Exception er){
          Console.WriteLine("INNER ERROR");
          Console.WriteLine(er);
        }
      }
    }

    public static async Task CreateAndScheduleJobDetail(BulkFileQueue record)
    {
      // Remove job if one already exists that has failed out
      if(await BulkDataScheduler.CheckExists(new JobKey(record.JobId.ToString()))){
        await BulkDataScheduler.DeleteJob(new JobKey(record.JobId.ToString()));
      }

      IJobDetail job = JobBuilder.Create<BulkDataFile>()
        .WithIdentity(new JobKey(record.JobId.ToString()))
        .Build(); //

        job.JobDataMap.Add("job_id", record.JobId);
        job.JobDataMap.Add("format", "text/csv");
        job.JobDataMap.Add("url", record.Url);
        job.JobDataMap.Add("fileName", record.FileName);
        job.JobDataMap.Add("StateCode", record.StateCode);
        job.JobDataMap.Add("DataType", record.DataType);
        job.JobDataMap.Add("DataSubType", record.SubType);
        job.JobDataMap.Add("Year", record.Year);
        job.JobDataMap.Add("Quarter", record.Quarter);
        job.JobDataMap.Add("ProgramCode", record.ProgramCode);

      ITrigger trigger = TriggerBuilder.Create()
        .StartNow()
        .Build();

      Console.WriteLine("SCHEDULED NEW JOB");

      await BulkDataScheduler.ScheduleJob(job, trigger);
    }

    public static void setScheduler(IScheduler scheduler){
      BulkDataFile.BulkDataScheduler = scheduler;
    }

  }
}