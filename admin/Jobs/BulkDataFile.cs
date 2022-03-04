using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.IO;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
using System.Collections.Generic;

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

    public BulkDataFile(NpgSqlContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      Configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {

      IAmazonS3 s3Client = new AmazonS3Client(
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_ACCESS_KEY_ID"],
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_SECRET_ACCESS_KEY"],
        RegionEndpoint.USGovCloudWest1
      );

      try
      {

        string url = ((string) context.JobDetail.JobDataMap.Get("url"));

        Console.Write("EXECUTING STREAM OF: " + url);
        
        //url = "https://api-easey-dev.app.cloud.gov/emissions-mgmt/apportioned/daily/stream?beginDate=2020-01-01&endDate=2020-01-03&programCode=ARP";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-api-key", Configuration["QUARTZ_API_KEY"]);
            client.DefaultRequestHeaders.Add("accept", (string) context.JobDetail.JobDataMap.Get("format"));

            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)){
              using(Stream s = await response.Content.ReadAsStreamAsync()){
                List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
                
                InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
                    Key = (string) context.JobDetail.JobDataMap.Get("fileName")
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
                        Key = (string) context.JobDetail.JobDataMap.Get("fileName"),
                        UploadId = initResponse.UploadId,
                        PartNumber = uploadPartNumber,
                        PartSize = bufferSize,
                        InputStream = new MemoryStream(bytes)
                    };

                    uploadResponses.Add(await s3Client.UploadPartAsync(uploadRequest));
                    await s.FlushAsync();

                    uploadPartNumber++;
                }

                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"],
                    Key = (string) context.JobDetail.JobDataMap.Get("fileName"),
                    UploadId = initResponse.UploadId
                };
                completeRequest.AddPartETags(uploadResponses);

                // Complete the upload.
                CompleteMultipartUploadResponse completeUploadResponse =
                    await s3Client.CompleteMultipartUploadAsync(completeRequest);
            }  
          }
        }

        Console.Write("STREAMED DATA SUCCESSFULLY");
        
        //return Task.CompletedTask;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        //return null;
      }
    }

    public static IJobDetail CreateJobDetail()
    {
      return JobBuilder.Create<BulkDataFile>().Build();
    }

    public static JobKey WithJobKey()
    {
      return new JobKey(Identity.JobName, Identity.Group);
    }

  }
}