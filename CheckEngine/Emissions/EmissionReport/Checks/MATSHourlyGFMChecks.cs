using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cMATSHourlyGFMChecks : cEmissionsChecks
    {

        #region Constructors

        public cMATSHourlyGFMChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)

        {
            CheckProcedures = new dCheckProcedure[8];

            CheckProcedures[1] = new dCheckProcedure(MatsGfm1);
            CheckProcedures[2] = new dCheckProcedure(MatsGfm2);
            CheckProcedures[3] = new dCheckProcedure(MatsGfm3);
            CheckProcedures[4] = new dCheckProcedure(MatsGfm4);
            CheckProcedures[5] = new dCheckProcedure(MatsGfm5);
            CheckProcedures[6] = new dCheckProcedure(MatsGfm6);
            CheckProcedures[7] = new dCheckProcedure(MatsGfm7);
        }

        #endregion


        #region Checks 1-10

        /// <summary>
        /// Component Id Valid
        /// 
        /// Ensure that the component id is associated with a sampling train.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsGfmSamplingTrainRecords = null;
               emParams.MatsHourlyGfmComponentIdValid = false;
               emParams.MatsSamplingTrainRecord = null;
               emParams.MatsSamplingTrainQaStatus = null;
               emParams.MatsSorbentTrapBeginDatehour = null;
               emParams.MatsSorbentTrapEndDatehour = null;
               emParams.MatsSamplingTrainCount = null;

                if (emParams.MatsHourlyGfmRecord.ComponentId == null)
                {
                    category.CheckCatalogResult = "A";
                }
                else
                {
                    CheckDataView<MatsSamplingTrainRecord> locatedMatsSamplingTrainRecords
                      =emParams.MatsSamplingTrainRecords.FindRows
                        (
                          new cFilterCondition("COMPONENT_ID",emParams.MatsHourlyGfmRecord.ComponentId),
                          new cFilterCondition("BEGIN_DATEHOUR",emParams.CurrentDateHour, eFilterDataType.DateBegan, eFilterConditionRelativeCompare.LessThanOrEqual),
                          new cFilterCondition("END_DATEHOUR",emParams.CurrentDateHour, eFilterDataType.DateEnded, eFilterConditionRelativeCompare.GreaterThanOrEqual)
                        // Sort by Begin Hour and End Hour handled in load of underlying source table
                        );

                   emParams.MatsSamplingTrainCount = locatedMatsSamplingTrainRecords.Count;
                   emParams.MatsGfmSamplingTrainRecords = locatedMatsSamplingTrainRecords;

                    if (locatedMatsSamplingTrainRecords.Count == 0)
                    {
                        category.CheckCatalogResult = "B";
                    }
                    else
                    {
                       emParams.MatsHourlyGfmComponentIdValid = true;
                       emParams.MatsSamplingTrainRecord = locatedMatsSamplingTrainRecords[0];
                       emParams.MatsSamplingTrainQaStatus =emParams.MatsSamplingTrainRecord.TrainQaStatusCd;
                       emParams.MatsSorbentTrapBeginDatehour =emParams.MatsSamplingTrainRecord.BeginDatehour;
                       emParams.MatsSorbentTrapEndDatehour =emParams.MatsSamplingTrainRecord.EndDatehour;
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
        /// Begin and End Hour Flags Valid
        /// 
        /// Check that Begin and End Hour Flags are valid..
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm2(cCategory category, ref bool log)
        {
            string returnVal = "";
            List<string> MonSysIdList = new List<string>();

            try
            {
                if (emParams.MatsHourlyGfmComponentIdValid.Default(false))
                {
                    if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "I")
                    {
                        if ((emParams.CurrentDateHour !=emParams.MatsSorbentTrapBeginDatehour) &&
                            (emParams.CurrentDateHour !=emParams.MatsSorbentTrapBeginDatehour.Value.AddHours(1)))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "F")
                    {
                        if ((emParams.CurrentDateHour !=emParams.MatsSorbentTrapEndDatehour) &&
                            (emParams.CurrentDateHour !=emParams.MatsSorbentTrapEndDatehour.Value.AddHours(-1)))
                        {
                            category.CheckCatalogResult = "B";
                        }
                    }
                    else if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == null)
                    {
                        if (emParams.CurrentDateHour ==emParams.MatsSorbentTrapBeginDatehour)
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else if (emParams.CurrentDateHour ==emParams.MatsSorbentTrapEndDatehour)
                        {
                            category.CheckCatalogResult = "D";
                        }
                    }
                    else if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "T")
                    {
                        if (emParams.MatsSamplingTrainCount <= 1)
                        {
                            // Current hour does not have two active traps/trains.
                            category.CheckCatalogResult = "E";
                        }
                        else if (emParams.CurrentDateHour !=emParams.MatsSorbentTrapEndDatehour)
                        {
                            // Current hour is not the end hour of the current sorbent trap.
                            category.CheckCatalogResult = "F";
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
        /// Hourly GFM Reading Valid
        /// 
        /// Hourly GFM Reading Null or Reported to Two Decimal Places
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsHourlyGfmComponentIdValid.Default(false))
                {
                    if (emParams.MatsHourlyGfmRecord.GfmReading == null)
                    {
                        if (emParams.MatsSamplingTrainQaStatus.NotInList("INC,EXPIRED,LOST") && (emParams.MatsHourlyGfmRecord.BeginEndHourFlg != "N"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "N")
                        {
                            category.CheckCatalogResult = "D";
                        }
                        else if (emParams.MatsSamplingTrainQaStatus.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsHourlyGfmRecord.GfmReading != Math.Round(emParams.MatsHourlyGfmRecord.GfmReading.Value, 2, MidpointRounding.AwayFromZero))
                        {
                            category.CheckCatalogResult = "C";
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
        /// Average Hourly Sampling Rate Valid
        /// 
        /// Average Hourly Sampling Rate Null or Reported to Two Decimal Places
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsHourlyGfmComponentIdValid.Default(false))
                {
                    if (emParams.MatsHourlyGfmRecord.AvgSamplingRate == null)
                    {
                        if (emParams.MatsSamplingTrainQaStatus.NotInList("INC,EXPIRED,LOST") && (emParams.MatsHourlyGfmRecord.BeginEndHourFlg != "N"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "N")
                        {
                            category.CheckCatalogResult = "D";
                        }
                        else if (emParams.MatsSamplingTrainQaStatus.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsHourlyGfmRecord.AvgSamplingRate != Math.Round(emParams.MatsHourlyGfmRecord.AvgSamplingRate.Value, 2, MidpointRounding.AwayFromZero))
                        {
                            category.CheckCatalogResult = "C";
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
        /// Sampling Rate UOM Valid
        /// 
        /// Sampling Rate UOM Null or Matches UOM Code
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsHourlyGfmComponentIdValid.Default(false))
                {
                    if (emParams.MatsHourlyGfmRecord.SamplingRateUom == null)
                    {
                        if (emParams.MatsSamplingTrainQaStatus.NotInList("INC,EXPIRED,LOST") && (emParams.MatsHourlyGfmRecord.BeginEndHourFlg != "N"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "N")
                        {
                            category.CheckCatalogResult = "D";
                        }
                        else if (emParams.MatsSamplingTrainQaStatus.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsHourlyGfmRecord.SamplingRateUom.NotInList("CCMIN,DSCMMIN,LMIN,CCHR,DSCMHR,LHR"))
                        {
                            category.CheckCatalogResult = "C";
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
        /// Average Hourly Sampling Rate Valid
        /// 
        /// Average Hourly Sampling Rate Null or Reported to Two Decimal Places
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsGfm6(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsHourlyGfmComponentIdValid.Default(false))
                {
                    if (emParams.MatsHourlyGfmRecord.FlowToSamplingRatio == null)
                    {
                        if (emParams.MatsSamplingTrainQaStatus.NotInList("INC,EXPIRED,LOST,FAILED") &&
                            ((emParams.CurrentFlowMonitorHourlyRecord != null) &&emParams.CurrentFlowMonitorHourlyRecord.ModcCd.InList("01,02,03,04,20,53,54")) &&
                            (emParams.MatsHourlyGfmRecord.BeginEndHourFlg != "N"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "N")
                        {
                            category.CheckCatalogResult = "G";
                        }
                        else if (emParams.MatsSamplingTrainQaStatus.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.CurrentFlowMonitorHourlyRecord == null)
                        {
                            category.CheckCatalogResult = "F";
                        }
                        else if (emParams.CurrentFlowMonitorHourlyRecord.ModcCd.NotInList("01,02,03,04,20,53,54"))
                        {
                            category.CheckCatalogResult = "E";
                        }
                        else if (emParams.MatsHourlyGfmRecord.FlowToSamplingRatio != Math.Round(emParams.MatsHourlyGfmRecord.FlowToSamplingRatio.Value, 1, MidpointRounding.AwayFromZero))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else if ((emParams.MatsHourlyGfmRecord.FlowToSamplingRatio < 1) || (100 <emParams.MatsHourlyGfmRecord.FlowToSamplingRatio))
                        {
                            category.CheckCatalogResult = "D";
                        }
                        else if (emParams.MatsHourlyGfmComponentIdValid.Default(false) &&
                                emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId) &&
                                 (emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].ReferenceSFSRRatio.Default(0) != 0))
                        {
                            decimal referenceRatio =emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].ReferenceSFSRRatio.Value;
                            decimal hourlySfsrRatioDev = Math.Round(Math.Abs(1 - (emParams.MatsHourlyGfmRecord.FlowToSamplingRatio.Value / referenceRatio)) * 100, 0, MidpointRounding.AwayFromZero);

                           emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].TotalSFSRRatioCount++;

                            if (hourlySfsrRatioDev > 25)
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].DeviatedSFSRRatioCount++;
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
        /// Updates the total hours that a GFM exists for a sampling train and the count of hours where the Begin and End Flag equals "N".
        /// </summary>
        /// <param name="category">Category object for the category in which the check is running.</param>
        /// <param name="log">obsolete</param>
        /// <returns></returns>
        public  string MatsGfm7(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsGfmSamplingTrainRecords != null)
                {
                    foreach (MatsSamplingTrainRecord samplingTrainRecord in emParams.MatsGfmSamplingTrainRecords)
                    {
                        if (samplingTrainRecord.TrainQaStatusCd.InList("PASSED,FAILED,UNCERTAIN") && (samplingTrainRecord.RataInd.Default(0) == 0))
                        {
                            if (emParams.MatsSamplingTrainDictionary.ContainsKey(samplingTrainRecord.TrapTrainId))
                            {
                                SamplingTrainEvalInformation dictionaryEntry =emParams.MatsSamplingTrainDictionary[samplingTrainRecord.TrapTrainId];
                                {
                                    dictionaryEntry.TotalGfmCount += 1;

                                    if (emParams.MatsHourlyGfmRecord.BeginEndHourFlg == "N")
                                    {
                                        dictionaryEntry.NotAvailablelGfmCount += 1;
                                    }
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

        #endregion

    }
}