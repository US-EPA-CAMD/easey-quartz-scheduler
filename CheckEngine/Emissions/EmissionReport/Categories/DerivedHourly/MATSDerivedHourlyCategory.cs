using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;
using ECMPS.ErrorSuppression;


namespace ECMPS.Checks.EmissionsReport
{
  /// <summary>
  /// Summary description for MATSDerivedHourlyCategory.
  /// </summary>
  public class cMATSDerivedHourlyCategory : cCategoryHourly
  {

    #region Constructors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ACheckEngine"></param>
    /// <param name="AHourlyEmissionsData"></param>
    /// <param name="ACategory"></param>
    /// <param name="ACategoryCode"></param>
    /// <param name="parameterCd"></param>
    public cMATSDerivedHourlyCategory(cCheckEngine ACheckEngine,
                                      cEmissionsReportProcess AHourlyEmissionsData,
                                      cOperatingHourCategory ACategory,
                                      string ACategoryCode, 
                                      string parameterCd, ref EmParameters emparams )
      : base(ACheckEngine,
       (cEmissionsReportProcess)AHourlyEmissionsData,
       ACategory,
       ACategoryCode, ref emparams)
    {
      ParameterCd = parameterCd;
            
    }


    #endregion


    #region Public Properties

    /// <summary>
    /// The parameter code of the associated monitor or derived hourly data.
    /// </summary>
    public string ParameterCd { get; protected set; }

       

        #endregion


        #region Base Class Overrides

        protected override void FilterData()
    {
    }

    protected override int[] GetDataBorderModcList()
    {
      return null;
    }

    protected override int[] GetQualityAssuranceHoursModcList()
    {
      return null;
    }

    protected override void SetRecordIdentifier()
    {
      string locationName = (emParams.CurrentMonitorPlanLocationRecord != null) ? emParams.CurrentMonitorPlanLocationRecord.LocationName : null;

      RecordIdentifier = string.Format("Location {0}, Parameter {1}", locationName, ParameterCd);
    }

    protected override bool SetErrorSuppressValues()
    {
      if (emParams.CurrentMonitorPlanLocationRecord != null && emParams.CurrentHourlyOpRecord != null)
      {
        long facId = CheckEngine.FacilityID;
        string locationName = emParams.CurrentMonitorPlanLocationRecord.LocationName;
        string matchDataValue = ParameterCd;
        DateTime? matchTimeValue = emParams.CurrentHourlyOpRecord.BeginDatehour.Default();

        ErrorSuppressValues = new cErrorSuppressValues(facId, locationName, "PARAM", matchDataValue, "HOUR", matchTimeValue);
        return true;
      }
      else
        return false;
    }

    #endregion

  }
}
