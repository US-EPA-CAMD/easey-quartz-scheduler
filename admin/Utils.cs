using System;
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
  }
}