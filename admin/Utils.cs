using System;
using System.Runtime.InteropServices;

namespace Epa.Camd.Quartz.Scheduler
{
  public static class Utils
  {
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
  }
}