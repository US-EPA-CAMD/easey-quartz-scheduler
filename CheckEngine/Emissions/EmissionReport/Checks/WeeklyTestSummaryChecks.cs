using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.EmissionsChecks
{
  public class WeeklyTestSummaryChecks : cEmissionsChecks
  {

    #region Constructors

    public WeeklyTestSummaryChecks(cEmissionsReportProcess emissionReportProcess)
      : base(emissionReportProcess)
    {
      CheckProcedures = new dCheckProcedure[14];

      CheckProcedures[1] = new dCheckProcedure(EMWTS1);
      CheckProcedures[2] = new dCheckProcedure(EMWTS2);
      CheckProcedures[3] = new dCheckProcedure(EMWTS3);
      CheckProcedures[4] = new dCheckProcedure(EMWTS4);
      CheckProcedures[5] = new dCheckProcedure(EMWTS5);
      CheckProcedures[6] = new dCheckProcedure(EMWTS6);
      CheckProcedures[7] = new dCheckProcedure(EMWTS7);
      CheckProcedures[8] = new dCheckProcedure(EMWTS8);
      CheckProcedures[9] = new dCheckProcedure(EMWTS9);
    }

    #endregion


    #region Check Methods ()

    /// <summary>
    /// Initialize Parameters
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS1(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        emParams.WeeklyTestSummaryValid = true;
        emParams.CalculatedWeeklyTestSummaryResult = null;
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Type
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS2(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        if (emParams.CurrentWeeklyTestSummary.TestTypeCd != "HGSI1")
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test System
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS3(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        if (emParams.CurrentWeeklyTestSummary.TestTypeCd == "HGSI1")
        {
          if (emParams.CurrentWeeklyTestSummary.MonSysId != null)
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "A";
          }
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Component
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS4(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        if (emParams.CurrentWeeklyTestSummary.TestTypeCd == "HGSI1")
        {
          if (emParams.CurrentWeeklyTestSummary.ComponentId == null)
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "A";
          }
          else if (emParams.CurrentWeeklyTestSummary.ComponentTypeCd != "HG")
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "B";
          }
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Date
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS5(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        emParams.TestDateValid = false;

        if (emParams.CurrentWeeklyTestSummary.TestDate == null)
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
        else if ((emParams.CurrentWeeklyTestSummary.TestDate < (new DateTime(1993, 1, 1))) ||
                 (emParams.CurrentWeeklyTestSummary.TestDate > emParams.CurrentReportingPeriodEndHour))
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "B";
        }
        else
        {
          emParams.TestDateValid = true;
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Hour
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS6(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        emParams.TestHourValid = false;

        if (emParams.CurrentWeeklyTestSummary.TestHour == null)
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
        else if ((emParams.CurrentWeeklyTestSummary.TestHour < 0) || (23 < emParams.CurrentWeeklyTestSummary.TestHour))
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "B";
        }
        else
        {
          emParams.TestHourValid = emParams.TestDateValid;
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Minute
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS7(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        emParams.TestDateTimeValid = false;

        if (emParams.CurrentWeeklyTestSummary.TestMin == null)
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
        else if ((emParams.CurrentWeeklyTestSummary.TestMin < 0) || (59 < emParams.CurrentWeeklyTestSummary.TestMin))
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "B";
        }
        else if (emParams.TestHourValid.Default(false))
        {
          DateTime calcTestDateTime = emParams.CurrentWeeklyTestSummary.TestDate.Value.AddHours(emParams.CurrentWeeklyTestSummary.TestHour.Value).AddMinutes(emParams.CurrentWeeklyTestSummary.TestMin.Value);

          if (emParams.CurrentWeeklyTestSummary.TestDatetime != calcTestDateTime)
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "C";
          }
          else
          {
            emParams.TestDateTimeValid = true;
          }
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Span Scale
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS8(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        if (emParams.CurrentWeeklyTestSummary.SpanScaleCd == null)
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
        else if (emParams.CurrentWeeklyTestSummary.SpanScaleCd.NotInList("H,M,L"))
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "B";
        }
        else if (emParams.CurrentWeeklyTestSummary.TestTypeCd == "HGSI1")
        {
          if (emParams.CurrentWeeklyTestSummary.SpanScaleCd != "H")
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "C";
          }
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }


    /// <summary>
    /// Check Weekly Test Result
    /// </summary>
    /// <param name="category">Category Object</param>
    /// <param name="log">Indicates whether to log results.</param>
    /// <returns>Returns error message if check fails to run correctly.</returns>
    public  string EMWTS9(cCategory category, ref bool log)
    {
      string returnVal = "";

      try
      {
        emParams.TestResultValid = false;

        if (emParams.CurrentWeeklyTestSummary.TestResultCd == null)
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "A";
        }
        else if (emParams.CurrentWeeklyTestSummary.TestResultCd.NotInList(emParams.TestResultCodeList))
        {
          emParams.WeeklyTestSummaryValid = false;
          category.CheckCatalogResult = "B";
        }
        else if (emParams.CurrentWeeklyTestSummary.TestTypeCd == "HGSI1")
        {
          if (emParams.CurrentWeeklyTestSummary.TestResultCd.NotInList("PASSED,PASSAPS,FAILED"))
          {
            emParams.WeeklyTestSummaryValid = false;
            category.CheckCatalogResult = "C";
          }
          else
          {
            emParams.TestResultValid = true;
          }
        }
        else
        {
          emParams.TestResultValid = true;
        }
      }
      catch (Exception ex)
      {
        returnVal = category.CheckEngine.FormatError(ex);
      }

      return returnVal;
    }

    #endregion

  }
}
