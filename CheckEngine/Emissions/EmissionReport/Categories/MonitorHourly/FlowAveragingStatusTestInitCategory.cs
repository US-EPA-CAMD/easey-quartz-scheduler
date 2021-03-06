using System;

using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;
using ECMPS.ErrorSuppression;


namespace ECMPS.Checks.EmissionsReport
{

    /// <summary>
    /// Implements the check category that processes MHV records with MODC 47 and 48.
    /// 
    /// The records can contain CO2C, NOXC and O2C parameters and are reported when
    /// a unit has a combined-cycle turbine with a primary (stack) and primary bypass
    /// (stack) reported at the the unit as systems.  In hours where both stacks operated
    /// the source reports one of the stacks and that stacks NOXC and diluent (CO2C or O2C)
    /// records normally, and reports the other stacks NOXC and diluent with either MODC 47
    /// (measured data) or 48 (missing data).
    /// </summary>
    public class cFlowAveragingStatusTestInitCategory : cCategoryHourly
    {

        #region Constructors

        public cFlowAveragingStatusTestInitCategory(cFlowMonitorHourlyCategory parentCategory)
            : base(parentCategory, "FLWAV")
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
            string locationName = (EmParameters.CurrentMonitorPlanLocationRecord != null) ? EmParameters.CurrentMonitorPlanLocationRecord.LocationName : null;
            string parameterCd = (EmParameters.CurrentFlowMonitorHourlyRecord != null) ? EmParameters.CurrentFlowMonitorHourlyRecord.ParameterCd : null;
            string modcCd = (EmParameters.CurrentFlowMonitorHourlyRecord != null) ? EmParameters.CurrentFlowMonitorHourlyRecord.ModcCd : null;

            RecordIdentifier = string.Format("Location {0}, Parameter {1}, MODC {2}", locationName, parameterCd, modcCd);
        }

        protected override bool SetErrorSuppressValues()
        {
            if (EmParameters.CurrentMonitorPlanLocationRecord != null && EmParameters.CurrentHourlyOpRecord != null && EmParameters.CurrentMonitorPlanLocationRecord != null)
            {
                long facId = CheckEngine.FacilityID;
                string locationName = EmParameters.CurrentMonitorPlanLocationRecord.LocationName;
                DateTime? matchTimeValue = EmParameters.CurrentHourlyOpRecord.BeginDatehour.Default();

                ErrorSuppressValues = new cErrorSuppressValues(facId, locationName, "PARAM", null, "HOUR", matchTimeValue);
                return true;
            }
            else
                return false;
        }

        #endregion

    }

}
