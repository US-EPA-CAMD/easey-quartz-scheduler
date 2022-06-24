using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Epa.Camd.Quartz.Scheduler.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Epa.Camd.Quartz.Scheduler
{
  public static class Utils
  {

    private static readonly HttpClient client = new HttpClient();

    public static DateTime getCurrentEasternTime(){
        TimeZoneInfo easternStandardTime;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            easternStandardTime = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        else
        {
            easternStandardTime = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }

        return TimeZoneInfo.ConvertTime (DateTime.Now, easternStandardTime);
    }

    public async static Task<string> validateRequestCredentials(HttpRequest Request, IConfiguration Configuration){

      string bearer = Request.Headers["Authorization"];
      string clientId = Request.Headers["x-client-id"];

      var split = bearer.Split(" ");

      if(bearer == null || split.Length != 2 || split[0] != "Bearer" || clientId == null)
        return "clientId and bearer clientToken required";
      try{
        ClientTokenValidation payload = new ClientTokenValidation();
        payload.clientId = clientId;
        payload.clientToken = split[1];

        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/client/validate", httpContent);
        response.EnsureSuccessStatusCode();
      }catch(Exception e){
        Console.WriteLine(e.ToString());
        return "Error validating clientId and clientToken";
      }

      return "";
    }

  }
}