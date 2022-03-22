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

    public async Task Execute(IJobExecutionContext context)
    {
      string url =  (string) context.JobDetail.JobDataMap.Get("url");
      string fileName = (string) context.JobDetail.JobDataMap.Get("fileName");
      Guid job_id = (Guid) context.JobDetail.JobDataMap.Get("job_id");

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
        bulkFile.StartDate = DateTime.Now;
        _dbContext.JobLogs.Update(bulkFile);
        await _dbContext.SaveChangesAsync();

        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        myHttpWebRequest.Headers.Add("x-api-key", Configuration["QUARTZ_API_KEY"]);
        myHttpWebRequest.Headers.Add("accept", (string) context.JobDetail.JobDataMap.Get("format"));
        myHttpWebRequest.KeepAlive = true;

        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

        Stream s = myHttpWebResponse.GetResponseStream();
        
        List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
        
        InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
            Key = fileName
        };

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
            await s.FlushAsync();

            uploadPartNumber++;
        }

        myHttpWebResponse.Close();
        myHttpWebRequest.Abort();

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

        bulkFile.StatusCd = "COMPLETE";
        bulkFile.EndDate = DateTime.Now;
        _dbContext.JobLogs.Update(bulkFile);
        _dbContext.SaveChanges();

        LogHelper.info("Executed stream successfully", new LogVariable("url", url));      
      }
      catch (Exception e)
      {
        LogHelper.error(e.Message);
        bulkFile.StatusCd = "ERROR";
        bulkFile.EndDate = DateTime.Now;
        bulkFile.AdditionalDetails = e.Message;
        _dbContext.JobLogs.Update(bulkFile);
        _dbContext.SaveChanges();
      }
    }

    public static IJobDetail CreateJobDetail(string key)
    {
      return JobBuilder.Create<BulkDataFile>().WithIdentity(new JobKey(key, Constants.QuartzGroups.BULK_DATA)).Build();
    }

  }
}