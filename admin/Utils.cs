using System;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Epa.Camd.Quartz.Scheduler.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace Epa.Camd.Quartz.Scheduler
{
  public static class Utils
  {

    public static IConfiguration Configuration {get; set;}

    public static DateTime getCurrentEasternTime(){
        try{
          return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        }catch(Exception){
          return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        }
    }

    public static string getBearerTokenFromRequest(HttpRequest Request){
      if(Request.Headers.ContainsKey("Authorization")){
        string token = Request.Headers["Authorization"];
        return token.Split(" ")[1];
      }else if(Request.Headers.ContainsKey("Bearer")){
        string token = Request.Headers["Bearer"];
        return token;
      }else{
        return null;
      }
    }

    public async static Task<string> validateRequestCredentialsClientToken(HttpRequest Request, IConfiguration Configuration){
      string bearer = getBearerTokenFromRequest(Request);
      if(bearer == null){
        return "Bearer clientToken required";
      }

      try{
        HttpClient client = new HttpClient();

        ClientTokenValidation payload = new ClientTokenValidation();
        payload.clientId = Configuration["EASEY_QUARTZ_SCHEDULER_CLIENT_ID"];

        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/client/validate", httpContent);
        Console.WriteLine(response);

        response.EnsureSuccessStatusCode();
      }catch(Exception e){
        Console.WriteLine(e);
        return "Error validating clientId and clientToken";
      }

      return "";
    }

    public static string validateRequestCredentialsGatewayToken(HttpRequest Request, IConfiguration Configuration){
      
      if(Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"] == null || Configuration["EASEY_QUARTZ_SCHEDULER_ENABLE_SECRET_TOKEN"] == "false"){
        return "";
      }
      
      string key;
      if(Request.Headers.ContainsKey("x-secret-token")){
        key = Request.Headers["x-secret-token"];
      }else{
        return "Must go through the gateway to access this resource.";
      }

      string accessToken = Configuration["EASEY_QUARTZ_SCHEDULER_SECRET_TOKEN"];

      if(!key.Equals(accessToken)){
        return "Incorrect gateway token provided.";
      }

      return "";
    }

    public async static Task<string> validateRequestCredentialsUserToken(HttpRequest Request, IConfiguration Configuration){

      string bearer = getBearerTokenFromRequest(Request);
      if(bearer == null){
        return "Bearer userToken required";
      }

      try{
        HttpClient client = new HttpClient();

        ClientDto dto = new ClientDto();

        if(Request.Headers.ContainsKey("x-forwarded-for")){
          string ip = Request.Headers["x-forwarded-for"];
          dto.clientIp = ip.Split(",")[0];
        }

        StringContent httpContent = new StringContent(JsonConvert.SerializeObject(dto), System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("x-api-key", Configuration["EASEY_QUARTZ_SCHEDULER_API_KEY"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        HttpResponseMessage response = await client.PostAsync(Configuration["EASEY_AUTH_API"] + "/tokens/validate", httpContent);

        response.EnsureSuccessStatusCode();
      }catch(Exception e){
        Console.WriteLine(e.GetBaseException());
        return "Error validating user bearer token";
      }

      return "";
    }

  }
}