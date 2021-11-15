namespace Epa.Camd.Easey.JobScheduler
{
  public static class Constants
  {
    public static class QuartzGroups
    {
      public static readonly string DEFAULT = "DEFAULT";
      public static readonly string QUARTZ = "QUARTZ";
      public static readonly string EVALUATIONS = "EVALUATIONS";
      public static readonly string MAINTAINANCE = "MAINTAINANCE";
    }

    public static class JobDetails
    {
      public static readonly string SEND_EMAIL_KEY = "Send Email";
      public static readonly string SEND_EMAIL_GROUP = QuartzGroups.QUARTZ;
      public static readonly string SEND_EMAIL_DESCRIPTION = "Sends an email per the provided job data map.";


      public static readonly string EXPIRED_CHECK_OUTS_KEY = "Cleanup Expired Check-Outs";
      public static readonly string EXPIRED_CHECK_OUTS_GROUP = QuartzGroups.MAINTAINANCE;
      public static readonly string EXPIRED_CHECK_OUTS_DESCRIPTION = "Deletes all records from camdecmpswks.user_check_out table where the current_time - last_activity is greater than the maximum inactivity period.";


      public static readonly string CHECK_ENGINE_EVALUATION_KEY = "Check Engine Evaluation";
      public static readonly string CHECK_ENGINE_EVALUATION_GROUP = QuartzGroups.EVALUATIONS;
      public static readonly string CHECK_ENGINE_EVALUATION_DESCRIPTION = "Evaluates Monitor Plan, QA Test/Certification, or Emissions data sets for accuracy as specified by the EPA Part 75 reporting instructions.";


      public static readonly string EXPIRED_USER_SESSIONS_KEY = "Cleanup Expired User Sessions";
      public static readonly string EXPIRED_USER_SESSIONS_GROUP = QuartzGroups.MAINTAINANCE;
      public static readonly string EXPIRED_USER_SESSIONS_DESCRIPTION = "Deletes all records from camdecmpswks.user_session table where the token_expiration < current_time resulting in an invalid session.";
    }

    public static class TriggerDetails
    {
      public static readonly string EXPIRED_CHECK_OUTS_KEY = "Cleanup Expired Check-Outs every 15 minutes";
      public static readonly string EXPIRED_CHECK_OUTS_GROUP = QuartzGroups.MAINTAINANCE;
      public static readonly string EXPIRED_CHECK_OUTS_DESCRIPTION = "Cleanup Expired Check-Outs on the 15th, 30th, & 45th minute of each hour & on the hour..";


      public static readonly string EXPIRED_USER_SESSIONS_KEY = "Cleanup Expired User Sessions every 15 mins";
      public static readonly string EXPIRED_USER_SESSIONS_GROUP = QuartzGroups.MAINTAINANCE;
      public static readonly string EXPIRED_USER_SESSIONS_DESCRIPTION = "Cleanup Expired User Sessions on the 15th, 30th, & 45th minute of each hour & on the hour.";
      
    }
  }
}