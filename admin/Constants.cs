namespace Epa.Camd.Quartz.Scheduler
{
  public static class Constants
  {
    public static class QuartzGroups
    {
      public static readonly string DEFAULT = "DEFAULT";
      public static readonly string QUARTZ = "QUARTZ";
      public static readonly string EVALUATIONS = "EVALUATIONS";
      public static readonly string BULK_DATA = "BULK_DATA";
      public static readonly string MAINTAINANCE = "MAINTAINANCE";
    }

    public static class EmailTemplateIds
    {
      public static readonly int SUBMISSION_FAILURE_SUPPORT = 203;
    }
  }
}
