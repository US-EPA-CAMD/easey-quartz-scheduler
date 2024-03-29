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

  public class WeeklySystemIntegrityTestCategory : cCategoryHourly
  {

        #region Constructors
      
        /// <summary>
        /// Creates a category with a specific parent category and category code.
        /// </summary>
        /// <param name="parentCategory">The parent category of the new category.</param>
        /// <param name="categoryCd">The category code of the new category.</param>
        /// <param name="componentIdentifier">The component identifier associated with the test.</param>
        /// <param name="componentTypeCd">The type of the component associated with the test.</param>
        /// <param name="testDateHour">The date and hour of the test.</param>
        public WeeklySystemIntegrityTestCategory(cCategory parentCategory, ref EmParameters emparams)
      : base(parentCategory, "WSI", "WeeklySystemIntegrity", ref emparams)
    {
          
        }

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
      if (emParams.CurrentWeeklySystemIntegrityTest != null)
        RecordIdentifier = string.Format("Location {0}, Component {1} and Type {2} at {3}",
                                         emParams.CurrentMonitorPlanLocationRecord.LocationName,
                                         emParams.CurrentWeeklySystemIntegrityTest.ComponentIdentifier,
                                         emParams.CurrentWeeklySystemIntegrityTest.ComponentTypeCd,
                                         emParams.CurrentWeeklySystemIntegrityTest.TestDatehour);
      else
        RecordIdentifier = "this test";
    }

    protected override bool SetErrorSuppressValues()
    {
      ErrorSuppressValues = new cErrorSuppressValues(CheckEngine.FacilityID,
                                                     emParams.CurrentMonitorPlanLocationRecord.LocationName,
                                                     "COMPTYP", emParams.CurrentWeeklySystemIntegrityTest.ComponentTypeCd,
                                                     "HOUR", emParams.CurrentWeeklySystemIntegrityTest.TestDatehour);
      return true;
    }

    #endregion

  }

}
