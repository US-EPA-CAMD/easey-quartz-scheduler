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

    public static IConfiguration Configuration {get; set;}

    public static DateTime getCurrentEasternTime(){
        try{
          return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        }catch(Exception){
          return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        }
    }

    public async static Task<string> validateRequestCredentialsClientToken(HttpRequest Request, IConfiguration Configuration){

      string[] split;
      if(Request.Headers.ContainsKey("Authorization")){
        string token = Request.Headers["Authorization"];
        split = token.Split(" ");
      }else{
        string token = Request.Headers["Bearer"];
        split = new string[]{"Bearer", token};
      }

      string clientId = Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"];

      if(split.Length != 2 || split[0] != "Bearer")
        return "Bearer clientToken required";
      try{
        ClientTokenValidation payload = new ClientTokenValidation();
        payload.clientId = clientId;
        payload.clientToken = split[1];

        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/client/validate", httpContent);
        Console.Write(response.Content);
        response.EnsureSuccessStatusCode();
      }catch(Exception e){
        Console.WriteLine(e.ToString());
        return "Error validating clientId and clientToken";
      }

      return "";
    }

    public async static Task<string> validateRequestCredentialsUserToken(HttpRequest Request, IConfiguration Configuration){

      string[] split;
      if(Request.Headers.ContainsKey("Authorization")){
        string token = Request.Headers["Authorization"];
        split = token.Split(" ");
      }else{
        string token = Request.Headers["Bearer"];
        split = new string[]{"Bearer", token};
      }

      if(split.Length != 2 || split[0] != "Bearer")
        return "User bearer token required";
      try{
        UserTokenValidation payload = new UserTokenValidation();
        payload.token = split[1];
        payload.clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        if(Request.Headers.ContainsKey("x-forwarded-for")){
          string ip = Request.Headers["x-forwarded-for"];
          payload.clientIp = ip.Split(",")[0];
        }
        
        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/validate", httpContent);

        Console.WriteLine(response.Content);

        response.EnsureSuccessStatusCode();
      }catch(Exception e){
        Console.WriteLine(e.GetBaseException());
        return "Error validating user bearer token";
      }

      return "";
    }

  }
}