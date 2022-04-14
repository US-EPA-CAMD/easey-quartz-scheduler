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

    [HttpGet()]
    public async Task<ActionResult> ListObjects()
    {
      ListObjectsV2Response response = null;
      List<BulkFile> files = new List<BulkFile>();

      try
      {
        programs = dbContext.ProgramCodes.ToList();
        ListObjectsV2Request request = new ListObjectsV2Request
        {
          BucketName = Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"]
        };

        response = await client.ListObjectsV2Async(request);

        foreach (S3Object entry in response.S3Objects)
        {
          BulkFile file = await this.CreateBulkFile(entry);
          if(file != null)
            files.Add(await this.CreateBulkFile(entry));
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

    private async Task<BulkFile> CreateBulkFile(S3Object entry)
    {
      try{
        string key = entry.Key;
        string[] keyParts = key.Split('/');
        string filename = keyParts[keyParts.Length - 1];

        GetObjectMetadataResponse response = await client.GetObjectMetadataAsync(Configuration["EASEY_QUARTZ_SCHEDULER_BULK_DATA_S3_BUCKET"], key);

        Dictionary<string, object> meta = new Dictionary<string, object>(); 

        foreach(string metaKey in response.Metadata.Keys) {  
          string strippedKey = metaKey.Substring(11);
          Console.Write(strippedKey);
          meta.Add(strippedKey, response.Metadata[metaKey]);
        } 

        return new BulkFile
        {
          S3Path = entry.Key,
          Filename = filename,
          Bytes = entry.Size,
          LastUpdated = entry.LastModified.ToUniversalTime(),
          Metadata = meta
        };
      }
      catch(Exception e){
        Console.Write(e.Message);
        return null;
      }
    }
  }
}