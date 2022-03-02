using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Epa.Camd.Quartz.Scheduler.Models;

namespace Epa.Camd.Quartz.Scheduler
{
  [ApiController]
  [Produces("application/json")]
  [Route("quartz-mgmt/bulk-files")]
  public class BulkDataController : ControllerBase
  {
    private static IAmazonS3 client;
    private IConfiguration Configuration { get; }

    public BulkDataController(IConfiguration configuration)
    {
      Configuration = configuration;
      client = new AmazonS3Client(
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_ACCESS_KEY_ID"],
        Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_AWS_SECRET_ACCESS_KEY"],
        RegionEndpoint.USGovCloudWest1
      );
    }

    [HttpGet()]
    public async Task<ActionResult> ListObjects()
    {
      ListObjectsV2Response response = null;
      List<BulkFile> files = new List<BulkFile>();

      try
      {
        ListObjectsV2Request request = new ListObjectsV2Request
        {
          BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"]
        };

        response = await client.ListObjectsV2Async(request);

        foreach (S3Object entry in response.S3Objects)
        {
          var key = entry.Key;
          var parts = key.Split('/');

          var file = new BulkFile {
            Filename = parts[parts.Length-1],
            S3Path = entry.Key,
            DataType = parts[0],
            DataSubType = parts[1],
            Grouping = parts[2],
            Bytes = entry.Size,
            LastUpdated = entry.LastModified.ToUniversalTime(),
          };

          files.Add(file);
        }
      }
      catch (AmazonS3Exception amazonS3Exception)
      {
        Console.WriteLine("S3 error occurred. Exception: " + amazonS3Exception.ToString());
        Console.ReadKey();
      }
      catch (Exception e)
      {
        Console.WriteLine("Exception: " + e.ToString());
        Console.ReadKey();
      }
      
      return new OkObjectResult(files);
    }
  }
}
