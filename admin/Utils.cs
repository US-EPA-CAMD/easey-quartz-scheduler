using System;
using System.Net.Http;
using System.Threading.Tasks;
using Epa.Camd.Quartz.Scheduler.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Epa.Camd.Quartz.Scheduler
{
  public static class Utils
  {

    public static IConfiguration Configuration {get; set;}

    public static TimeZoneInfo getCurrentEasternZone(){
      try{
          return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }catch(Exception){
          return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }

    public static DateTime getCurrentEasternTime(){
      return TimeZoneInfo.ConvertTime(DateTime.Now, getCurrentEasternZone());
    }

    public async static Task<string> generateClientToken(){
      try{
        HttpClient client = new HttpClient();

        ClientTokenGeneration payload = new ClientTokenGeneration();
        payload.clientId = Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"];
        payload.clientSecret = Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_SECRET"];
        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/client", httpContent);
        response.EnsureSuccessStatusCode();

        ClientTokenResponse tokenResponse = JsonConvert.DeserializeObject<ClientTokenResponse>(response.Content.ReadAsStringAsync().Result);
        return tokenResponse.token;
      }catch(Exception e){
        Console.WriteLine(e);
        throw new Exception("Error generating client token");
      }
    }
  }
}