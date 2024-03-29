﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;
using ECMPS.ErrorSuppression;

namespace ECMPS.Checks.EmissionsReport
{

  /// <summary>
  /// Generic version of the RATA Status Category class.
  /// 
  /// Includes the setting of the following data parameters:
  /// 
  /// * HourlyOperatingDataRecordsForLocation
  /// * LocationAttributeRecordsByHourLocation
  /// * TestExtensionExemptionRecords
  /// 
  /// </summary>
  public class GenericSystemBasedStatusCategory : cCategoryHourly
  {

    #region Constructors

    /// <summary>
    /// Creates a category with a specific parent category and category code.
    /// </summary>
    /// <param name="parentCategory">The parent category of the new category.</param>
    /// <param name="categoryCd">The category code of the new category.</param>
    /// <param name="parameterCd">The parameter code of the associated monitor or derived hourly data.</param>
    public GenericSystemBasedStatusCategory(cCategory parentCategory, string categoryCd, string parameterCd, ref EmParameters emparams)
      : base(parentCategory, categoryCd, ref emparams)
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
      DataView[] OperatingHoursByLocation = (DataView[])EmissionParameters.OperatingHoursByLocation.Value;

            //TODO: Eventual all updates of the following (including other categories) should move to the operating hour category.
            emParams.HourlyOperatingDataRecordsForLocation = new CheckDataView<VwMpHrlyOpDataRow>(OperatingHoursByLocation[CurrentMonLocPos]);
            emParams.LocationAttributeRecordsByHourLocation = new CheckDataView<VwMpLocationAttributeRow>(FilterRanged("LocationAttribute", LocationAttribute, CurrentOpDate, CurrentOpHour, CurrentMonLocId, "Begin_Date", "End_Date"));
            emParams.TestExtensionExemptionRecords = new CheckDataView<VwQaTestExtensionExemptionRow>(FilterLocation("TEERecords", TEERecords, CurrentMonLocId));
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
      if (emParams.QaStatusSystemIdentifier != null)
        RecordIdentifier = "Monitor System ID " + emParams.QaStatusSystemIdentifier;
      else
        RecordIdentifier = "Monitor System ID null";
    }

    protected override bool SetErrorSuppressValues()
    {
      if (emParams.CurrentMonitorPlanLocationRecord != null)
      {
        ErrorSuppressValues 
          = new cErrorSuppressValues
            (
              CheckEngine.FacilityID,
              emParams.CurrentMonitorPlanLocationRecord.LocationName,
              "PARAM", ParameterCd,
              "HOUR", emParams.CurrentDateHour.AsStartDateTime()
            );

        return true;
      }
      else
        return false;
    }

    #endregion

  }

}
