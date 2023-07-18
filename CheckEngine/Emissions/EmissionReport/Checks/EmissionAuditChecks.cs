using System;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.TypeUtilities;


namespace ECMPS.Checks.EmissionsChecks
{

    public class EmissionAuditChecks : cEmissionsChecks
    {

        #region Constructors

        public EmissionAuditChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[2];

            CheckProcedures[1] = new dCheckProcedure(EMAUDIT1);
        }

        #endregion Constructors


        #region Checks 1-10

        public  string EMAUDIT1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.LikeKindHours = null;

                if (emParams.ComponentRecordForAudit.ComponentIdentifier.StartsWith("LK"))
                {
                    int? locationPosition = emParams.LocationPositionLookup.ContainsKey(emParams.ComponentRecordForAudit.MonLocId)
                                          ? emParams.LocationPositionLookup[emParams.ComponentRecordForAudit.MonLocId]
                                          : (int?)null;

                    if (emParams.ComponentOperatingSuppDataDictionaryArray[locationPosition.Value].ContainsKey(emParams.ComponentRecordForAudit.ComponentId))
                    {
                        int hours = emParams.ComponentOperatingSuppDataDictionaryArray[locationPosition.Value]
                                                                                             [emParams.ComponentRecordForAudit.ComponentId]
                                                                                             .QuarterlyOperatingCounts
                                                                                             .Hours;

                        if (hours > 0)
                        {
                            emParams.LikeKindHours = hours;

                            CheckDataView<ComponentOpSuppData> supplementalRecords
                                = emParams.ComponentOperatingSuppDataRecordsForMpAndYear
                                              .FindRows(new cFilterCondition("COMPONENT_ID", emParams.ComponentRecordForAudit.ComponentId),
                                                        new cFilterCondition("CALENDAR_YEAR", emParams.CurrentReportingPeriodObject.Year),
                                                        new cFilterCondition("QUARTER", eFilterConditionRelativeCompare.LessThan, (int)emParams.CurrentReportingPeriodObject.Quarter),
                                                        new cFilterCondition("OP_SUPP_DATA_TYPE_CD", "OP"));

                            foreach (ComponentOpSuppData supplementalRecord in supplementalRecords)
                            {
                                emParams.LikeKindHours += supplementalRecord.Hours;
                            }

                            if (emParams.LikeKindHours > 720)
                            {
                                category.CheckCatalogResult = "A";
                            }
                            else
                            {
                                category.CheckCatalogResult = "B";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        #endregion Checks 1-10



    }

}
