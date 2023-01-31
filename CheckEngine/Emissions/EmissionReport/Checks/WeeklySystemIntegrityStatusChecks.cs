using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.EmissionsChecks
{

    public class WeeklySystemIntegrityStatusChecks : cEmissionsChecks
    {

        #region Constructors

        public WeeklySystemIntegrityStatusChecks(cEmissionsReportProcess emissionReportProcess)
          : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[6];

            CheckProcedures[1] = new dCheckProcedure(WSISTAT1);
            CheckProcedures[2] = new dCheckProcedure(WSISTAT2);
            CheckProcedures[3] = new dCheckProcedure(WSISTAT3);
            CheckProcedures[4] = new dCheckProcedure(WSISTAT4);
            CheckProcedures[5] = new dCheckProcedure(WSISTAT5);
        }

        #endregion


        #region Check Methods ()

        /// <summary>
        /// Initialize Status Checking
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string WSISTAT1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.WsiStatus = null;
                emParams.WsiPluginEventRecord = null;
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Locate Prior Test
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string WSISTAT2(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.WsiTestDictionary.ContainsKey(emParams.QaStatusComponentId))
                {
                    emParams.WsiPriorTestRecord = emParams.WsiTestDictionary[emParams.QaStatusComponentId].MostRecentTestRecord;
                }
                else
                {
                    emParams.WsiPriorTestRecord = null;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Check for Intervening Event
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string WSISTAT3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.WsiInterveningEventRecord = null;

                if (emParams.WsiStatus == null)
                {
                    /* WsiPriorTestRecord should exist if WsiStatus is null. */

                    VwQaCertEventRow interveningEventRecord
                      = emParams.QaCertificationEventRecords.FindMostRecentRow(
                                                                                    emParams.CurrentDateHour.Value,
                                                                                    "QA_CERT_EVENT_DATEHOUR",
                                                                                    new cFilterCondition[]
                                                                                    {
                                                                            new cFilterCondition("COMPONENT_ID", emParams.QaStatusComponentId),
                                                                            new cFilterCondition("QA_CERT_EVENT_DATEHOUR", emParams.WsiPriorTestRecord.TestDatehour, eFilterDataType.DateEnded, eFilterConditionRelativeCompare.GreaterThan),
                                                                            new cFilterCondition("QA_CERT_EVENT_CD", "110,130", eFilterConditionStringCompare.InList)
                                                                                    }
                                                                                  );

                    if (interveningEventRecord != null)
                    {
                        emParams.WsiInterveningEventRecord = interveningEventRecord;
                        emParams.WsiPluginEventRecord = interveningEventRecord;
                        emParams.WsiStatus = "OOC-Event";
                    }
                    else
                    {
                        if (emParams.WsiPriorTestRecord.TestResultCd == null)
                        {
                            emParams.WsiStatus = "OOC-Test Has Critical Errors";
                        }
                        else if (emParams.WsiPriorTestRecord.TestResultCd == "FAILED")
                        {
                            emParams.WsiStatus = "OOC-Test Failed";
                        }
                        else
                        {
                            WsiTestStatusInformation wsiTestEntry = emParams.WsiTestDictionary[emParams.QaStatusComponentId];
                            {
                                if ((wsiTestEntry.OperatingDateList != null) && (wsiTestEntry.OperatingDateList.Count > 7))
                                {
                                    emParams.WsiStatus = "OOC-Expired";
                                }
                                else
                                {
                                    emParams.WsiStatus = "IC";
                                }
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

        /// <summary>
        /// Return the Final Status
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string WSISTAT4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (!emParams.WsiStatus.StartsWith("IC"))
                {
                    category.CheckCatalogResult = emParams.WsiStatus;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Check for Intervening Like-Kind Event
        /// 
        /// Locates a prior event with:
        /// 
        /// 1) component id equalts the QA status component id,
        /// 2) event code equals 140 or 141, 
        /// 3) event date and hour preceeds the current hour,
        /// 4) and when a prior test does not exist event date and hour is after the previous test's dates.
        /// 
        /// If the event is found:
        /// 
        /// 1) if at least seven operating days occurred between the event and the current hour, an OOC-NoPriorTest result occurs.
        /// 2) Otherwise, an IC-Undetermined result occurs.
        /// 
        /// If the even is not found and a prior test does not exist:
        /// 
        /// 1) If at least seven operating days have occurred in the quarter and since the begin data of the system component record 
        ///    of the componenet, an OOC-NoPriorTest result occurs.
        /// 2) Otherwise, an IC-Undetermined result occurs.
        /// 
        /// If the even is not found and a prior test does exists, no result is produced.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string WSISTAT5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.WsiInterveningLikeKindEventRecord = null;

                cFilterCondition[] filterCondition;
                {
                    if (emParams.WsiPriorTestRecord == null)
                        filterCondition = new cFilterCondition[]
                                            {
                                  new cFilterCondition("COMPONENT_ID", emParams.QaStatusComponentId),
                                  new cFilterCondition("QA_CERT_EVENT_CD", "140,141", eFilterConditionStringCompare.InList)
                                            };
                    else
                        filterCondition = new cFilterCondition[]
                                            {
                                  new cFilterCondition("COMPONENT_ID", emParams.QaStatusComponentId),
                                  new cFilterCondition("QA_CERT_EVENT_CD", "140,141", eFilterConditionStringCompare.InList),
                                  new cFilterCondition("QA_CERT_EVENT_DATEHOUR", emParams.WsiPriorTestRecord.TestDatehour.AsEndDateTime(),
                                                       eFilterDataType.DateBegan, eFilterConditionRelativeCompare.GreaterThan)
                                            };
                }

                VwQaCertEventRow interveningEventRecord
                  = emParams.QaCertificationEventRecords.FindMostRecentRow(emParams.CurrentDateHour.Value, "QA_CERT_EVENT_DATEHOUR", filterCondition);

                if (interveningEventRecord != null)
                {
                    emParams.WsiInterveningLikeKindEventRecord = interveningEventRecord;

                    DateTime earliestOperatingDate = emParams.WsiInterveningLikeKindEventRecord.QaCertEventDate.Default(DateTime.MinValue).AddDays(1);

                    if (emParams.OperatingDateArray[emParams.CurrentMonitorPlanLocationPostion.Value].Count(item => item >= earliestOperatingDate) > 7)
                    {
                        if (emParams.WsiPriorTestRecord == null)
                        {
                            emParams.WsiStatus = "OOC-No Prior Test";
                        }
                        else
                        {
                            emParams.WsiPluginEventRecord = emParams.WsiInterveningLikeKindEventRecord;
                            emParams.WsiStatus = "OOC-Event";
                        }
                    }
                    else
                    {
                        emParams.WsiStatus = "IC-Undetermined";
                    }
                }
                else
                {
                    if (emParams.WsiPriorTestRecord == null)
                    {
                        DateTime earliestOperatingDate;
                        {
                            if ((emParams.QaStatusMatsErbDate != null) && (emParams.QaStatusMatsErbDate.Default(DateTime.MinValue) > emParams.QaStatusComponentBeginDate.Default(DateTime.MinValue)))
                                earliestOperatingDate = emParams.QaStatusMatsErbDate.Value.AddDays(1);
                            else
                                earliestOperatingDate = emParams.QaStatusComponentBeginDate.Default(DateTime.MinValue).AddDays(1);
                        }

                        if (emParams.OperatingDateArray[emParams.CurrentMonitorPlanLocationPostion.Value].Count(item => item >= earliestOperatingDate) > 7)
                        {
                            emParams.WsiStatus = "OOC-No Prior Test";
                        }
                        else
                        {
                            emParams.WsiStatus = "IC-Undetermined";
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

        #endregion

    }

}
