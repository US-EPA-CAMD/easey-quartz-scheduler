using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Checks.EmissionsReport;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.EmissionsChecks
{
    public class cLeakStatusChecks : cEmissionsChecks
    {
        #region Constructors

        /// <summary>
        /// Creates and instance of the Flow-to-Load checks object and populates its CheckProcedures array.
        /// </summary>
        /// <param name="emissionReportProcess">The owning emission report process object.</param>
        public cLeakStatusChecks(cEmissionsReportProcess emissionReportProcess)
          : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[6];

            CheckProcedures[1] = new dCheckProcedure(LKSTAT1);
            CheckProcedures[2] = new dCheckProcedure(LKSTAT2);
            CheckProcedures[3] = new dCheckProcedure(LKSTAT3);
            CheckProcedures[4] = new dCheckProcedure(LKSTAT4);
            CheckProcedures[5] = new dCheckProcedure(LKSTAT5);
        }


        /// <summary>
        /// Constructor used for testing.
        /// </summary>
        /// <param name="emissionParameters"></param>
        public cLeakStatusChecks(cEmissionsCheckParameters emissionParameters)
        {
            EmManualParameters = emissionParameters;
        }

        #endregion

        #region Checks

        #region Checks (1 - 10)

        /// <summary>
        /// LKSTAT1: Locate Most Recent Prior Leak Check 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public string LKSTAT1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                PriorLeakRecord.SetValue(null, category);

                string componentId = emParams.QaStatusComponentId;
                DateTime currentHour = CurrentMhvRecord.Value["BEGIN_DATE"].AsDateTime(DateTime.MinValue).AddHours(CurrentMhvRecord.Value["BEGIN_HOUR"].AsInteger(0));

                DataRowView leakCheckRecord = cRowFilter.FindMostRecentRow(
                                                                           LeakCheckRecordsByLocationForQAStatus.Value,
                                                                           currentHour.Date,
                                                                           currentHour.Hour,
                                                                           new cFilterCondition[]
                                                                             {
                                                                       new cFilterCondition("COMPONENT_ID", componentId),
                                                                       new cFilterCondition("TEST_RESULT_CD", "INVALID", true)
                                                                             }
                                                                          );

                if (leakCheckRecord != null)
                {
                    PriorLeakRecord.SetValue(leakCheckRecord, category);
                }

            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// LKSTAT2: Locate Most Recent Prior Event
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public string LKSTAT2(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                PriorLeakEventRecord.SetValue(null, category);
                LeakStatusResult.SetValue(null, category);

                string componentId = emParams.QaStatusComponentId;
                DateTime currentHour = CurrentMhvRecord.Value["BEGIN_DATE"].AsDateTime(DateTime.MinValue).AddHours(CurrentMhvRecord.Value["BEGIN_HOUR"].AsInteger(0));
                cReportingPeriod currentQuarter = new cReportingPeriod(CurrentReportingPeriod.Value.Default(0));


                if (PriorLeakRecord.Value == null)
                {
                    DataRowView qaCertificationEventRow = cRowFilter.FindMostRecentRow(
                                                                                        QaCertificationEventRecords.Value,
                                                                                        currentQuarter.BeganDate.AddHours(-1).Date,
                                                                                        currentQuarter.BeganDate.AddHours(-1).Hour,
                                                                                        "QA_CERT_EVENT_DATE", "QA_CERT_EVENT_HOUR",
                                                                                        new cFilterCondition[]
                                                                                        {
                                                                                new cFilterCondition("COMPONENT_ID", componentId),
                                                                                new cFilterCondition("QA_CERT_EVENT_CD", "300,305", eFilterConditionStringCompare.InList)
                                                                                        }
                                                                                      );

                    if (qaCertificationEventRow != null)
                    {
                        PriorLeakEventRecord.SetValue(qaCertificationEventRow, category);

                        cReportingPeriod expectedLeakCheckQuarter = new cReportingPeriod(PriorLeakEventRecord.Value["QA_CERT_EVENT_DATE"].AsEndDateTime()).AddQuarter(1);

                        LeakStatusResult.SetValue(LkStat2_QuarterCheck(expectedLeakCheckQuarter), category);
                    }
                    else
                    {
                        DataRowView componentRecord = cRowFilter.FindEarliestRow(MpSystemComponentRecords.Value,
                                                                                 new cFilterCondition[]
                                                                                 {
                                                                       new cFilterCondition("COMPONENT_ID", componentId)
                                                                                 }
                                                                                );
                        if ((componentRecord != null) && (componentRecord["BEGIN_DATE"].AsBeginDateTime() >= CurrentReportingPeriodBeginHour.Value.Value) &&
                            (componentRecord["BEGIN_DATE"].AsBeginDateTime() <= CurrentReportingPeriodEndHour.Value.Value))
                        {
                            LeakStatusResult.SetValue("IC", category);
                        }
                        else
                        {
                            LeakStatusResult.SetValue("OOC-No Prior Test", category);
                        }
                    }
                }
                else//PriorLeakRecord Found
                {
                    DataRowView qaCertificationEventRow = cRowFilter.FindMostRecentRow(
                                                                                        QaCertificationEventRecords.Value,
                                                                                        currentHour.AddHours(-1).Date,
                                                                                        currentHour.AddHours(-1).Hour,
                                                                                        "QA_CERT_EVENT_DATE", "QA_CERT_EVENT_HOUR",
                                                                                        new cFilterCondition[]
                                                                                        {
                                                                                new cFilterCondition("COMPONENT_ID", componentId),
                                                                                new cFilterCondition("LEAK_REQUIRED", "Y"),
                                                                                new cFilterCondition("QA_CERT_EVENT_CD", "300", true),
                                                                                new cFilterCondition("QA_CERT_EVENT_DATEHOUR", PriorLeakRecord.Value["END_DATEHOUR"].AsEndDateTime(), eFilterDataType.DateBegan, eFilterConditionRelativeCompare.GreaterThan)
                                                                                        }
                                                                                      );

                    if (qaCertificationEventRow != null)
                    {
                        PriorLeakEventRecord.SetValue(qaCertificationEventRow);

                        cReportingPeriod expectedLeakCheckQuarter = new cReportingPeriod(PriorLeakEventRecord.Value["QA_CERT_EVENT_DATE"].AsEndDateTime()).AddQuarter(1);

                        LeakStatusResult.SetValue(LkStat2_QuarterCheck(expectedLeakCheckQuarter), category);
                    }
                    else if (PriorLeakRecord.Value["QA_NEEDS_EVAL_FLG"].AsString() == "Y")
                    {
                        LeakStatusResult.SetValue("Prior Test Not Yet Evaluated", category);
                    }
                    else if (PriorLeakRecord.Value["TEST_RESULT_CD"].AsString() == null)
                    {
                        LeakStatusResult.SetValue("OOC-Test Has Critical Errors", category);
                    }
                    else if (PriorLeakRecord.Value["TEST_RESULT_CD"].AsString() == "FAILED")
                    {
                        LeakStatusResult.SetValue("OOC-Test Failed", category);
                    }
                    else if (PriorLeakRecord.Value["TEST_RESULT_CD"].AsString() == "ABORTED")
                    {
                        LeakStatusResult.SetValue("OOC-Test Aborted", category);
                    }
                }

            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        #region LKSTAT2 Helper Methods

        /// <summary>
        /// Check the quarters starting with the expectedLeakCheckQuarter and ending with the the earlier of the quarter before the CurrentReportingPeriod
        /// or the fourth quarter checked to find the first QA operating quarter.  If a non-op quarter is not found, returns "IC".  Otherwise, sums the 
        /// operating hours in the first non-op quarter and the remaining unchecked quarters and returns "OOC-Event" if the sum is greater than or equal 
        /// to 168 and otherwise returns "EC-Crace".
        ///  
        /// For Ozone Season reports, only quarters 2 and 3 are checked.
        /// </summary>
        /// <param name="expectedLeakCheckQuarter">The quarter for which a Leak Check is expected.</param>
        /// <returns></returns>
        public string LkStat2_QuarterCheck(cReportingPeriod expectedLeakCheckQuarter)
        {
            cReportingPeriod currentReportingPeriod = new cReportingPeriod(emParams.CurrentReportingPeriod.Default(0));
            cReportingPeriod requiredLeakCheckQuarter = null;


            int quartersAfterEventCount = 0;

            // Determine the first quarter (exclusively) between the event and current quarters that requires a leak check.
            for (cReportingPeriod quarter = expectedLeakCheckQuarter; quarter.CompareTo(currentReportingPeriod) < 0; quarter = quarter.AddQuarter(1))
            {
                if (emParams.AnnualReportingRequirement.Default(true) || (quarter.Quarter.AsInteger() == 2 || quarter.Quarter.AsInteger() == 3))
                {
                    VwMpOpSuppDataRow operatingSuppDataRecord
                        = emParams.OperatingSuppDataRecordsByLocation.FindRow(new cFilterCondition[]
                                                                                  {
                                                                                            new cFilterCondition("OP_TYPE_CD", emParams.AnnualReportingRequirement.Default(false) || quarter.Quarter.AsInteger() == 3
                                                                                                                               ? "OPHOURS"
                                                                                                                               : "OSHOURS"),
                                                                                            new cFilterCondition("FUEL_CD", ""),
                                                                                            new cFilterCondition("RPT_PERIOD_ID", quarter.RptPeriodId.AsString())
                                                                                  });

                    if (operatingSuppDataRecord == null)
                    {
                        requiredLeakCheckQuarter = quarter;
                        break;
                    }
                    else if (operatingSuppDataRecord.OpValue >= 168)
                    {
                        requiredLeakCheckQuarter = quarter;
                        break;
                    }
                }

                quartersAfterEventCount += 1;

                if (emParams.AnnualReportingRequirement.Default(true) && (quartersAfterEventCount == 4))
                {
                    requiredLeakCheckQuarter = quarter;
                    break;
                }
            }


            string result;
            {
                if (requiredLeakCheckQuarter == null)
                {
                    result = "IC";
                }
                else if (!emParams.AnnualReportingRequirement.Default(true))
                {
                    result = "OOC-Event";
                }
                else
                {
                    int graceOpHours = emParams.RptPeriodOpHoursAccumulatorArray[emParams.CurrentMonitorPlanLocationPostion.Value];

                    if (graceOpHours > 168)
                    {
                        result = "OOC-Event";
                    }
                    else
                    {
                        for (cReportingPeriod quarter = requiredLeakCheckQuarter.AddQuarter(1); quarter.CompareTo(currentReportingPeriod) < 0; quarter = quarter.AddQuarter(1))
                        {
                            if (emParams.AnnualReportingRequirement.Default(true) || (quarter.Quarter.AsInteger() == 2 || quarter.Quarter.AsInteger() == 3))
                            {
                                VwMpOpSuppDataRow operatingSuppDataRecord
                                    = emParams.OperatingSuppDataRecordsByLocation.FindRow(new cFilterCondition[]
                                                                                              {
                                                                                                new cFilterCondition("OP_TYPE_CD", emParams.AnnualReportingRequirement.Default(false) || (quarter.Quarter.AsInteger() == 3) ? "OPHOURS" : "OSHOURS"),
                                                                                                new cFilterCondition("FUEL_CD", ""),
                                                                                                new cFilterCondition("RPT_PERIOD_ID", quarter.RptPeriodId.AsString())
                                                                                              });

                                if ((operatingSuppDataRecord != null) && (operatingSuppDataRecord.OpValue != null))
                                {
                                    graceOpHours += (int)operatingSuppDataRecord.OpValue;
                                }

                                if (graceOpHours > 168)
                                {
                                    break;
                                }
                            }
                        }

                        result = (graceOpHours > 168) ? "OOC-Event" : "IC-Grace";
                    }
                }
            }

            return result;
        }

        #endregion


        /// <summary>
        /// LKSTAT3: Determine Expiration Date For Prior Leak Check 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public string LKSTAT3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (LeakStatusResult.Value == null)
                {
                    PriorLeakExpirationDate.SetValue(PriorLeakRecord.Value["TEST_EXP_DATE"].AsDateTime(), category);

                    //The priorLeakEndDate should never be null, if it is, then make it min date
                    DateTime priorLeakEndDate = PriorLeakRecord.Value["END_DATE"].AsBeginDateTime();

                    if (PriorLeakExpirationDate.Value == null)
                    {
                        if (AnnualReportingRequirement.Value.Default(true) == false)
                        {

                            if (priorLeakEndDate.Quarter() == 2)
                            {
                                PriorLeakExpirationDate.SetValue(new DateTime(priorLeakEndDate.Year, 9, 30));
                            }
                            else
                            {
                                if (priorLeakEndDate.Quarter() == 1)
                                {
                                    PriorLeakExpirationDate.SetValue(new DateTime(priorLeakEndDate.Year, 6, 30));
                                }
                                else
                                {
                                    PriorLeakExpirationDate.SetValue(new DateTime(priorLeakEndDate.Year + 1, 6, 30));
                                }
                            }
                        }
                        else
                        {
                            if (PriorLeakRecord.Value["GP_IND"].AsInteger() == 1)
                            {
                                PriorLeakExpirationDate.SetValue((new DateTime(priorLeakEndDate.Year, 3 * (priorLeakEndDate.Quarter() - 1) + 1, 1)).AddMonths(3).AddDays(-1), category);
                            }
                            else
                            {
                                PriorLeakExpirationDate.SetValue((new DateTime(priorLeakEndDate.Year, 3 * (priorLeakEndDate.Quarter() - 1) + 1, 1)).AddMonths(6).AddDays(-1), category);
                            }
                        }
                    }

                    PriorLeakRecord.Value["TEST_EXP_DATE"] = PriorLeakExpirationDate.Value;

                    if (CurrentMhvRecord.Value["BEGIN_DATE"].AsEndDateTime() <= PriorLeakExpirationDate.Value.AsBeginDateTime())
                    {
                        LeakStatusResult.SetValue("IC", category);
                    }
                }
                else
                    PriorLeakExpirationDate.SetValue(null, category);

            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// LKSTAT4: Determine Extended Expiration Date for Prior Leak Check
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public string LKSTAT4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                LeakMissingOpDataInfo.SetValue(null, category);

                if (LeakStatusResult.Value == null)
                {
                    bool missingOpData = false;

                    if (PriorLeakRecord.Value["TEST_EXP_DATE_WITH_EXT"].AsDateTime() == null)
                    {
                        int numberOfExtensionQuarters = 0;

                        cReportingPeriod priorLeakExpirationQuarter = new cReportingPeriod(PriorLeakExpirationDate.Value.AsEndDateTime());
                        cReportingPeriod currentMhvQuarter = new cReportingPeriod(CurrentMhvRecord.Value["BEGIN_DATE"].AsBeginDateTime());

                        for (cReportingPeriod reportingPeriod = priorLeakExpirationQuarter;
                             reportingPeriod.CompareTo(currentMhvQuarter) < 0;
                             reportingPeriod = reportingPeriod.AddQuarter(1))
                        {

                            if (EarliestLocationReportDate.Value.AsEndDateTime() > reportingPeriod.EndedDate)
                            {
                                numberOfExtensionQuarters++;
                            }
                            else
                            {

                                if (AnnualReportingRequirement.Value.Default(false) == true || reportingPeriod.Quarter.AsInteger().InRange(2, 3))
                                {
                                    DataRowView operatingSuppDataRecords;
                                    {
                                        if (AnnualReportingRequirement.Value.Default(false) == true || reportingPeriod.Quarter == eQuarter.q3)
                                        {
                                            operatingSuppDataRecords = cRowFilter.FindRow(OperatingSuppDataRecordsByLocation.Value,
                                                                                          new cFilterCondition[]
                                                                                          {
                                                                      new cFilterCondition("OP_TYPE_CD", "OPHOURS"),
                                                                      new cFilterCondition("FUEL_CD", ""),
                                                                      new cFilterCondition("RPT_PERIOD_ID", reportingPeriod.RptPeriodId.AsString())
                                                                                          });
                                        }
                                        else
                                        {
                                            operatingSuppDataRecords = cRowFilter.FindRow(OperatingSuppDataRecordsByLocation.Value,
                                                                                          new cFilterCondition[]
                                                                                          {
                                                                      new cFilterCondition("OP_TYPE_CD", "OSHOURS"),
                                                                      new cFilterCondition("FUEL_CD", ""),
                                                                      new cFilterCondition("RPT_PERIOD_ID", reportingPeriod.RptPeriodId.AsString())
                                                                                          });
                                        }
                                    }

                                    if (operatingSuppDataRecords != null && operatingSuppDataRecords["OP_VALUE"].AsDecimal() < 168)
                                    {

                                        if (AnnualReportingRequirement.Value.Default(false) == true || reportingPeriod.Quarter == eQuarter.q2)
                                        {
                                            numberOfExtensionQuarters++;
                                        }
                                        else
                                        {
                                            numberOfExtensionQuarters = numberOfExtensionQuarters + 3;
                                        }
                                    }
                                    else if (operatingSuppDataRecords == null)
                                    {

                                        if (reportingPeriod.Quarter.AsInteger() == 1 || reportingPeriod.Quarter.AsInteger() == 4)
                                        {

                                            DataRowView locationReportingFrequencyRecords = cRowFilter.FindRow(LocationReportingFrequencyRecords.Value,
                                                                                          new cFilterCondition[]
                                                                                          {
                                                                      new cFilterCondition("REPORT_FREQ_CD", "OS"),
                                                                      new cFilterCondition("BEGIN_DATE", reportingPeriod.BeganDate, eFilterDataType.DateBegan, eFilterConditionRelativeCompare.LessThanOrEqual),
                                                                      new cFilterCondition("END_DATE", reportingPeriod.EndedDate, eFilterDataType.DateEnded, eFilterConditionRelativeCompare.GreaterThanOrEqual)
                                                                                          });

                                            if (locationReportingFrequencyRecords != null)
                                            {

                                                if (AnnualReportingRequirement.Value.Default(false) == true && reportingPeriod.Quarter.AsInteger() == 4 && locationReportingFrequencyRecords["END_DATE"].AsEndDateTime().Year == reportingPeriod.Year)
                                                {
                                                    numberOfExtensionQuarters++;
                                                }
                                                else
                                                    break;
                                            }
                                            else
                                            {
                                                missingOpData = true;
                                                LeakMissingOpDataInfo.SetValue((reportingPeriod.Year.AsString() + "Q" + reportingPeriod.Quarter.AsInteger()), category);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            missingOpData = true;
                                            LeakMissingOpDataInfo.SetValue((reportingPeriod.Year.AsString() + "Q" + reportingPeriod.Quarter.AsInteger()), category);
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }
                            }
                        }
                        PriorLeakExpirationDate.SetValue(PriorLeakExpirationDate.Value.AsEndDateTime().AddMonths(3 * numberOfExtensionQuarters), category);
                        PriorLeakRecord.Value["TEST_EXP_DATE_WITH_EXT"] = PriorLeakExpirationDate.Value;
                    }
                    else
                    {
                        PriorLeakExpirationDate.SetValue(PriorLeakRecord.Value["TEST_EXP_DATE_WITH_EXT"].AsDateTime(), category);
                    }

                    if (CurrentMhvRecord.Value["BEGIN_DATE"].AsEndDateTime() <= PriorLeakExpirationDate.Value.AsBeginDateTime())
                    {
                        LeakStatusResult.SetValue("IC-Extension", category);
                    }
                    else if (missingOpData == true)
                    {
                        LeakStatusResult.SetValue("Missing Op Data", category);
                        PriorLeakRecord.Value["TEST_EXP_DATE_WITH_EXT"] = DBNull.Value;
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
        /// LKSTAT5: Determine Grace Period for Leak Check
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public string LKSTAT5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                decimal GraceOpHours = 0;
                int locationPosition = CurrentMonitorPlanLocationPostion.Value.AsInteger(int.MinValue);
                int[] rptPeriodOpHoursAccumulatorArray = category.GetCheckParameter("Rpt_Period_Op_Hours_Accumulator_Array").ValueAsIntArray();

                if (LeakStatusResult.Value == null)
                {

                    if (AnnualReportingRequirement.Value == false)
                    {
                        LeakStatusResult.SetValue("OOC-Expired", category);
                    }
                    else if (rptPeriodOpHoursAccumulatorArray[locationPosition] == -1)
                    {
                        LeakStatusResult.SetValue("Invalid Op Data", category);
                    }
                    else
                    {
                        GraceOpHours = rptPeriodOpHoursAccumulatorArray[locationPosition];

                        if (GraceOpHours > 168)
                        {
                            LeakStatusResult.SetValue("OOC-Expired", category);
                        }
                        else
                        {

                            DateTime? latestDate = cDateFunctions.LatestDate(PriorLeakExpirationDate.Value.AsBeginDateTime(), EarliestLocationReportDate.Value.AsBeginDateTime());
                            int rptPeriodOfLastDateOfQtrAfterLatestDate = cDateFunctions.ThisReportingPeriod(cDateFunctions.LastDateNextQuarter(latestDate.Value));
                            int rptPeriodOfCurrentMHVRecord = cDateFunctions.ThisReportingPeriod(CurrentMhvRecord.Value["BEGIN_DATE"].AsBeginDateTime());
                            //DateTime currentHour = CurrentMhvRecord.Value["BEGIN_DATE"].AsDateTime(DateTime.MinValue).AddHours(CurrentMhvRecord.Value["BEGIN_HOUR"].AsInteger(0));

                            if (rptPeriodOfLastDateOfQtrAfterLatestDate == rptPeriodOfCurrentMHVRecord)
                            {
                                LeakStatusResult.SetValue("IC-Grace", category);
                            }
                            else
                            {
                                DataRowView operatingSuppDataRecords;
                                for (int i = rptPeriodOfLastDateOfQtrAfterLatestDate;
                                     i < rptPeriodOfCurrentMHVRecord;
                                     i++)
                                {

                                    if (EarliestLocationReportDate.Value.AsBeginDateTime() <= cDateFunctions.LastDateThisQuarter(i))
                                    {
                                        operatingSuppDataRecords = cRowFilter.FindRow(OperatingSuppDataRecordsByLocation.Value,
                                                                                      new cFilterCondition[]
                                                                                        {
                                                                      new cFilterCondition("OP_TYPE_CD", "OPHOURS"),
                                                                      new cFilterCondition("FUEL_CD", ""),
                                                                      new cFilterCondition("RPT_PERIOD_ID", i.AsString())
                                                                                        });

                                        if (operatingSuppDataRecords != null)
                                        {
                                            GraceOpHours = GraceOpHours + operatingSuppDataRecords["OP_VALUE"].AsDecimal(0);

                                            if (GraceOpHours > 168)
                                            {
                                                LeakStatusResult.SetValue("OOC-Expired", category);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            int myLeakMissingOpDataInfoYear;
                                            int myLeakMissingOpDataInfoQtr;

                                            cDateFunctions.GetYearAndQuarter(i, out myLeakMissingOpDataInfoYear, out myLeakMissingOpDataInfoQtr);
                                            LeakStatusResult.SetValue("Missing Op Data", category);
                                            LeakMissingOpDataInfo.SetValue(myLeakMissingOpDataInfoYear + "Q" + myLeakMissingOpDataInfoQtr, category);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (LeakStatusResult.Value == null)
                {
                    LeakStatusResult.SetValue("IC-Grace", category);
                }

                if (LeakStatusResult.Value.StartsWith("IC") == false)
                    category.CheckCatalogResult = LeakStatusResult.Value;
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }
        #endregion

        #endregion
    }
}