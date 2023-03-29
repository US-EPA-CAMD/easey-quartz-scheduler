using System;

using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;
using ECMPS.ErrorSuppression;


namespace ECMPS.Checks.EmissionsReport
{

    /// <summary>
    /// Initializes the RATA status processing for the primary or primary bypass NOX system for NOXR that was not reported.
    /// </summary>
    public class cNoxrUnusedPpbRataStatusInitCategory : cCategoryHourly
    {

        #region Constructors

        public cNoxrUnusedPpbRataStatusInitCategory(cNoxrUnusedPpbMonitorHourlyCategory parentCategory, ref EmParameters emparams)
            : base(parentCategory, "NXPPBRI", ref emparams)
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
            string locationName = (emParams.CurrentMonitorPlanLocationRecord != null) ? emParams.CurrentMonitorPlanLocationRecord.LocationName : null;
            string parameterCd = (emParams.CurrentNoxrPrimaryOrPrimaryBypassMhvRecord != null) ? emParams.CurrentNoxrPrimaryOrPrimaryBypassMhvRecord.ParameterCd : null;
            string modcCd = (emParams.CurrentNoxrPrimaryOrPrimaryBypassMhvRecord != null) ? emParams.CurrentNoxrPrimaryOrPrimaryBypassMhvRecord.ModcCd : null;

            RecordIdentifier = string.Format("Location {0}, Parameter {1}, MODC {2}", locationName, parameterCd, modcCd);
        }

        protected override bool SetErrorSuppressValues()
        {
            if (emParams.CurrentMonitorPlanLocationRecord != null && emParams.CurrentHourlyOpRecord != null && emParams.CurrentMonitorPlanLocationRecord != null)
            {
                long facId = CheckEngine.FacilityID;
                string locationName = emParams.CurrentMonitorPlanLocationRecord.LocationName;
                string matchDataValue = emParams.CurrentNoxrPrimaryOrPrimaryBypassMhvRecord.ParameterCd;
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
