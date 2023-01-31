using System;
using System.Data;
using System.Linq;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Enumerations;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cMatsSorbentTrapChecks : cEmissionsChecks
    {

        #region Constructors

        public cMatsSorbentTrapChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)

        {
            CheckProcedures = new dCheckProcedure[16];

            CheckProcedures[1] = new dCheckProcedure(MatsTrp1);
            CheckProcedures[2] = new dCheckProcedure(MatsTrp2);
            CheckProcedures[3] = new dCheckProcedure(MatsTrp3);
            CheckProcedures[4] = new dCheckProcedure(MatsTrp4);
            CheckProcedures[5] = new dCheckProcedure(MatsTrp5);
            CheckProcedures[6] = new dCheckProcedure(MatsTrp6);
            CheckProcedures[7] = new dCheckProcedure(MatsTrp7);
            CheckProcedures[8] = new dCheckProcedure(MatsTrp8);
            CheckProcedures[9] = new dCheckProcedure(MatsTrp9);
            CheckProcedures[10] = new dCheckProcedure(MatsTrp10);
            CheckProcedures[11] = new dCheckProcedure(MatsTrp11);
            CheckProcedures[12] = new dCheckProcedure(MatsTrp12);
            CheckProcedures[13] = new dCheckProcedure(MatsTrp13);
            CheckProcedures[14] = new dCheckProcedure(MatsTrp14);
            CheckProcedures[15] = new dCheckProcedure(MatsTrp15);
        }

        #endregion


        #region Checks 1-10

        /// <summary>
        /// Begin Date Valid
        /// 
        /// This check determines if the sorbent trap data begin date is valid.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapBeginDateValid = false;

                if (emParams.MatsSorbentTrapRecord.BeginDate == null)
                {
                    emParams.MatsSorbentTrapEvaluationNeeded = false;
                    category.CheckCatalogResult = "A";
                }
                else
                {
                    emParams.MatsSorbentTrapBeginDateValid = true;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Begin Hour Valid
        /// 
        /// This check determines if the sorbent trap data begin  hour is valid.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp2(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapBeginDateHourValid = false;

                if (emParams.MatsSorbentTrapBeginDateValid.Default(false))
                {
                    if (emParams.MatsSorbentTrapRecord.BeginHour == null)
                    {
                        emParams.MatsSorbentTrapEvaluationNeeded = false;
                        category.CheckCatalogResult = "A";
                    }
                    else if ((emParams.MatsSorbentTrapRecord.BeginHour < 0) || (23 < emParams.MatsSorbentTrapRecord.BeginHour))
                    {
                        emParams.MatsSorbentTrapEvaluationNeeded = false;
                        category.CheckCatalogResult = "B";
                    }
                    else
                    {
                        emParams.MatsSorbentTrapBeginDateHourValid = true;
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
        /// End Date Valid
        /// 
        /// This check determines if the sorbent trap data end date and  hour is valid.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapEndDateValid = false;

                if (emParams.MatsSorbentTrapRecord.EndDate == null)
                {
                    emParams.MatsSorbentTrapEvaluationNeeded = false;
                    category.CheckCatalogResult = "A";
                }
                else
                {
                    emParams.MatsSorbentTrapEndDateValid = true;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// End Hour Valid
        /// 
        /// This check determines if the sorbent trap data end hour is valid.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapEndDateHourValid = false;

                if (emParams.MatsSorbentTrapEndDateValid.Default(false))
                {
                    if (emParams.MatsSorbentTrapRecord.EndHour == null)
                    {
                        emParams.MatsSorbentTrapEvaluationNeeded = false;
                        category.CheckCatalogResult = "A";
                    }
                    else if ((emParams.MatsSorbentTrapRecord.EndHour < 0) || (23 < emParams.MatsSorbentTrapRecord.EndHour))
                    {
                        emParams.MatsSorbentTrapEvaluationNeeded = false;
                        category.CheckCatalogResult = "B";
                    }
                    else
                    {
                        emParams.MatsSorbentTrapEndDateHourValid = true;
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
        /// Begin and End Times Consistent
        /// 
        /// Check that the Sorbent Trap end date and time occurs after the begin date and time. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapDatesAndHoursConsistent = false;

                if (emParams.MatsSorbentTrapBeginDateHourValid.Default(false) && emParams.MatsSorbentTrapEndDateHourValid.Default(false))
                {
                    if (emParams.MatsSorbentTrapRecord.BeginDatehour.Value > emParams.MatsSorbentTrapRecord.EndDatehour.Value)
                    {
                        emParams.MatsSorbentTrapEvaluationNeeded = false;
                        category.CheckCatalogResult = "A";
                    }
                    else
                        emParams.MatsSorbentTrapDatesAndHoursConsistent = true;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Check For Overlap With Another Sorbent Trap
        /// 
        /// Check for overlap with the last Sorbent Trap from the previous emission report or 
        /// with another Sorbent Trap reported in the current emission report. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp6(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                CheckDataView<MatsSorbentTrapRecord> sorbentTrapRecords
                  = emParams.MatsSorbentTrapRecords.FindRows(new cFilterCondition[]
                                                                     {
                                                               new cFilterCondition("MON_SYS_ID", emParams.MatsSorbentTrapRecord.MonSysId),
                                                               new cFilterCondition("TRAP_ID", emParams.MatsSorbentTrapRecord.TrapId, true),
                                                               new cFilterCondition("BEGIN_DATEHOUR", emParams.MatsSorbentTrapRecord.EndDatehour, eFilterDataType.DateBegan, eFilterConditionRelativeCompare.LessThan),
                                                               new cFilterCondition("END_DATEHOUR", emParams.MatsSorbentTrapRecord.BeginDatehour, eFilterDataType.DateEnded, eFilterConditionRelativeCompare.GreaterThan)
                                                                     });

                if (sorbentTrapRecords.Count > 0)
                {
                    emParams.MatsSorbentTrapEvaluationNeeded = false;
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
        /// Initialize MATS Sorbent Trap Parameters
        /// 
        /// Initialize MATS Sampling Train Data.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp7(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapValidExists = false;
                emParams.MatsSorbentTrapSamplingTrainList = null;
                emParams.MatsSamplingTrainProblemComponentExists = false;

                SorbentTrapEvalInformation sorbentTrapEvalInformation = new SorbentTrapEvalInformation();
                {
                    sorbentTrapEvalInformation.SorbentTrapValidExists = true;
                    sorbentTrapEvalInformation.IsBorderTrap = (emParams.MatsSorbentTrapRecord.BorderTrapInd == 1);
                    sorbentTrapEvalInformation.IsSupplementalData = (emParams.MatsSorbentTrapRecord.SuppDataInd == 1);
                    sorbentTrapEvalInformation.SorbentTrapId = emParams.MatsSorbentTrapRecord.TrapId;
                    sorbentTrapEvalInformation.SorbentTrapBeginDateHour = emParams.MatsSorbentTrapRecord.BeginDatehour;
                    sorbentTrapEvalInformation.SorbentTrapEndDateHour = emParams.MatsSorbentTrapRecord.EndDatehour;
                    sorbentTrapEvalInformation.SorbentTrapModcCd = emParams.MatsSorbentTrapRecord.ModcCd;
                    sorbentTrapEvalInformation.SamplingTrainProblemComponentExists = false;
                    //Setting of SamplingTrainList and OperatingDateList happen through object references
                }

                string trapId = emParams.MatsSorbentTrapRecord.TrapId;

                emParams.MatsSorbentTrapDictionary[trapId] = sorbentTrapEvalInformation;
                emParams.MatsSorbentTrapListByLocationArray[emParams.CurrentMonitorPlanLocationPostion.Value].Add(sorbentTrapEvalInformation);

                emParams.MatsSorbentTrapValidExists = emParams.MatsSorbentTrapDictionary[trapId].SorbentTrapValidExists;
                emParams.MatsSorbentTrapSamplingTrainList = emParams.MatsSorbentTrapDictionary[trapId].SamplingTrainList;
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Monitoring System Check
        /// 
        /// This check determines if the sorbent trap data begin date is valid.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp8(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSorbentTrapRecord.MonSysId == null)
                {
                    emParams.MatsSorbentTrapValidExists = false;
                    category.CheckCatalogResult = "A";
                }
                else if (emParams.MatsSorbentTrapRecord.SysTypeCd != "ST")
                {
                    emParams.MatsSorbentTrapValidExists = false;
                    category.CheckCatalogResult = "B";
                }
                else if ((emParams.MatsSorbentTrapRecord.SystemBeginDatehour.Default(DateTime.MinValue) > emParams.MatsSorbentTrapRecord.BeginDatehour.Default(DateTime.MinValue)) ||
                         (emParams.MatsSorbentTrapRecord.SystemEndDatehour.Default(DateTime.MaxValue) < emParams.MatsSorbentTrapRecord.EndDatehour.Default(DateTime.MaxValue)))
                {
                    emParams.MatsSorbentTrapValidExists = false;
                    category.CheckCatalogResult = "C";
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Number and Validity of Sampling Trains
        /// 
        /// Check that two Sorbent Train Data Records are provided for each Sorbent Trap Data Record. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp9(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSamplingTrainsValid = false;

                if (emParams.MatsSamplingTrainProblemComponentExists == false)
                {
                    if (emParams.MatsSorbentTrapSamplingTrainList.Count != 2)
                    {
                        emParams.MatsSorbentTrapValidExists = false;
                        category.CheckCatalogResult = "A";
                    }
                    // Check that SamplingTrainValid for both sampling trains is equal to true.
                    else if (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.SamplingTrainValid.Default(false) == false) > 0)
                    {
                        emParams.MatsSorbentTrapValidExists = false;
                    }
                    else
                    {
                        emParams.MatsSamplingTrainsValid = true;
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
        /// Sorbent Trap MODC Code is Valid
        /// 
        /// Check Sorbent Trap MODC Code Valid. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp10(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapModcCodeValid = false;

                if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("01,02,32,33,34,35,43,44"))
                {
                    emParams.MatsSorbentTrapValidExists = false;
                    category.CheckCatalogResult = "A";
                }

                else if (emParams.MatsSamplingTrainsValid.Default(false)) // Insures that two valid sampling trains exist.
                {
                    if (emParams.MatsSorbentTrapRecord.ModcCd.InList("01,02,43"))
                    {
                        if (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "PASSED") == 2)
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "B";
                        }
                    }

                    else if (emParams.MatsSorbentTrapRecord.ModcCd.InList("32,44"))
                    {
                        if ((emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "PASSED") == 1) &&
                            (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode.InList("FAILED,LOST")) == 1))
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "C";
                        }
                    }

                    else if (emParams.MatsSorbentTrapRecord.ModcCd == "33")
                    {
                        if (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "UNCERTAIN") == 2)
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "D";
                        }
                    }

                    else if (emParams.MatsSorbentTrapRecord.ModcCd == "34")
                    {
                        if (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "FAILED") == 2)
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else if (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "UNCERTAIN") == 2)
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else if ((emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "LOST") >= 1) ||
                                 (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "EXPIRED") >= 1) ||
                                 (emParams.MatsSorbentTrapSamplingTrainList.Count(item => item.TrainQAStatusCode == "INC") >= 1))
                        {
                            emParams.MatsSorbentTrapModcCodeValid = true;
                        }
                        else
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "E";
                        }
                    }

                    else if (emParams.MatsSorbentTrapRecord.ModcCd == "35")
                    {
                        emParams.MatsSorbentTrapModcCodeValid = true;
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


        #region Checks 11-20

        /// <summary>
        /// Paired Trap Agreement Validation and Re-calculation
        /// 
        /// Determine if the Paired Trap Agreement is Valid. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp11(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapPairedTrapAgreementValid = false;
                emParams.MatsCalcTrapAbsoluteDifference = null;
                emParams.MatsCalcTrapPercentDifference = null;

                if (emParams.MatsSorbentTrapModcCodeValid.Default(false))
                {
                    /* Paired Trap Agreement is null */
                    if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement == null)
                    {
                        /* Check for bad MODC */
                        if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("32,34,35,44"))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "A";
                        }
                        /* Check for Absolute Difference Indicator not equal to null */
                        else if (emParams.MatsSorbentTrapRecord.AbsoluteDifferenceInd != null)
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "B";
                        }
                        else
                        {
                            emParams.MatsSorbentTrapPairedTrapAgreementValid = true;
                        }
                    }

                    /* Paired Trap Agreement is not null */
                    else
                    {
                        /* Check for bad MODC */
                        if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("01,02,33,43"))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "C";
                        }
                        /* Check for Paired Trap Agreement not rounded to 2 */
                        else if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement != Math.Round(emParams.MatsSorbentTrapRecord.PairedTrapAgreement.Value, 2, MidpointRounding.AwayFromZero))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "D";
                        }
                        /* Handle when absolute difference indicator is 0 or 1 (not null) */
                        else if ((emParams.MatsSorbentTrapRecord.AbsoluteDifferenceInd == 0) || (emParams.MatsSorbentTrapRecord.AbsoluteDifferenceInd == 1))
                        {
                            decimal hgConcentration1 = emParams.MatsSorbentTrapSamplingTrainList[0].HgConcentration.Value;
                            decimal hgConcentration2 = emParams.MatsSorbentTrapSamplingTrainList[1].HgConcentration.Value;

                            emParams.MatsCalcTrapAbsoluteDifference = Math.Abs(hgConcentration1 - hgConcentration2);
                            emParams.MatsCalcTrapPercentDifference = ((hgConcentration1 + hgConcentration2) != 0) ?  100 * emParams.MatsCalcTrapAbsoluteDifference / (hgConcentration1 + hgConcentration2) : 0m;
                            emParams.MatsCalcTrapAbsoluteDifference = Math.Round(emParams.MatsCalcTrapAbsoluteDifference.Value, 2, MidpointRounding.AwayFromZero);
                            emParams.MatsCalcTrapPercentDifference = Math.Round(emParams.MatsCalcTrapPercentDifference.Value, 2, MidpointRounding.AwayFromZero);

                            /* Check when Absolute Difference Indicator is 0 */
                            if (emParams.MatsSorbentTrapRecord.AbsoluteDifferenceInd == 0)
                            {
                                /* Check for Percent Difference Discrepancy */
                                if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement != emParams.MatsCalcTrapPercentDifference)
                                {
                                    emParams.MatsSorbentTrapValidExists = false;
                                    category.CheckCatalogResult = "G";
                                }

                                /* Handle when Paired Trap Agreement is less than or equal to 10 */
                                else if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement <= 10)
                                {
                                    if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("01,02,43"))
                                    {
                                        emParams.MatsSorbentTrapValidExists = false;
                                        category.CheckCatalogResult = "H";
                                    }
                                    else
                                    {
                                        emParams.MatsSorbentTrapPairedTrapAgreementValid = true;
                                    }
                                }

                                /* Handle when Paired Trap Agreement is less than or equal to 20, and concentration is less than or equal to 1.0 */
                                else if ((emParams.MatsSorbentTrapRecord.PairedTrapAgreement <= 20) &&
                                         (emParams.MatsSorbentTrapRecord.HgConcentration.ScientificNotationtoDecimal() <= 1.0m))
                                {
                                    if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("01,02,43"))
                                    {
                                        emParams.MatsSorbentTrapValidExists = false;
                                        category.CheckCatalogResult = "I";
                                    }
                                    else
                                    {
                                        emParams.MatsSorbentTrapPairedTrapAgreementValid = true;
                                    }
                                }

                                /* Handle Paired Trap Aggreement Failed */
                                else
                                {
                                    if (emParams.MatsSorbentTrapRecord.ModcCd != "33")
                                    {
                                        emParams.MatsSorbentTrapValidExists = false;
                                        category.CheckCatalogResult = "J";
                                    }
                                    else
                                    {
                                        emParams.MatsSorbentTrapPairedTrapAgreementValid = true;
                                    }
                                }
                            }

                            /* Check when Absolute Difference Indicator is 1 */
                            else
                            {
                                /* Ensure that Paired Trap Agreement is less than or equal to 0.03 */
                                if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement.Value <= 0.03m)
                                {
                                    /* Check for Absolute Difference Discrepancy */
                                    if (emParams.MatsSorbentTrapRecord.PairedTrapAgreement != emParams.MatsCalcTrapAbsoluteDifference)
                                    {
                                        emParams.MatsSorbentTrapValidExists = false;
                                        category.CheckCatalogResult = "E";
                                    }
                                    else
                                    {
                                        emParams.MatsSorbentTrapPairedTrapAgreementValid = true;
                                    }
                                }
                                else
                                {
                                    emParams.MatsSorbentTrapValidExists = false;
                                    category.CheckCatalogResult = "F";
                                }
                            }
                        }

                        /* Absolute Difference is null (not 0 or 1) */
                        else
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "K";
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
        /// Hg System Concentration Validation and Re-calculation
        /// 
        /// Determin whether the Hg System Concentration is valid. 
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp12(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsCalcHgSystemConcentration = null;

                if (emParams.MatsSorbentTrapPairedTrapAgreementValid.Default(false))
                {
                    if (emParams.MatsSorbentTrapRecord.HgConcentration == null)
                    {
                        if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("34,35"))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSorbentTrapRecord.ModcCd.NotInList("01,02,32,33,43,44"))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSorbentTrapRecord.HgConcentration.MatsSignificantDigitsValid(emParams.MatsSorbentTrapRecord.EndDate.Value))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "C";
                        }
                        else if (ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal(emParams.MatsSorbentTrapRecord.HgConcentration) == 0)
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "E";
                        }
                        else if (emParams.MatsSorbentTrapSamplingTrainList.Any(item => item.HgConcentration == 0 ))
                        {
                            emParams.MatsSorbentTrapValidExists = false;
                            category.CheckCatalogResult = "F";
                        }
                        else
                        {
                            decimal hgConcentrationCalculation;

                            if (emParams.MatsSorbentTrapRecord.ModcCd.InList("32,44"))
                            {
                                hgConcentrationCalculation = 1.111m * emParams.MatsSorbentTrapSamplingTrainList.First(train => train.TrainQAStatusCode == "PASSED").HgConcentration.Value;
                            }
                            else if (emParams.MatsSorbentTrapRecord.ModcCd == "33")
                            {
                                hgConcentrationCalculation = emParams.MatsSorbentTrapSamplingTrainList.Select(item => item.HgConcentration.Value).ToList().Max();
                            }
                            else // MODC 01, 02 or 43
                            {
                                hgConcentrationCalculation = emParams.MatsSorbentTrapSamplingTrainList.Sum(item => item.HgConcentration.Value) / 2;
                            }

                            emParams.MatsCalcHgSystemConcentration = hgConcentrationCalculation.MatsSignificantDigitsFormat(emParams.MatsSorbentTrapRecord.EndDate.Value, 
                                                                                                                                emParams.MatsSorbentTrapRecord.HgConcentration);

                            if (emParams.MatsSorbentTrapRecord.HgConcentration != emParams.MatsCalcHgSystemConcentration)
                            {
                                emParams.MatsSorbentTrapValidExists = false;
                                category.CheckCatalogResult = "D";
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
        /// Update Sorbent Trap Parameters
        /// 
        /// Update Sorbent Trap Dictionary for the current location with values from individual check parameters.
        /// The Sampling Train Dictionary and Operating Date List are updated through object referencing and therefore are update here.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp13(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsSorbentTrapDictionary[emParams.MatsSorbentTrapRecord.TrapId].SorbentTrapValidExists = emParams.MatsSorbentTrapValidExists;
                emParams.MatsSorbentTrapDictionary[emParams.MatsSorbentTrapRecord.TrapId].SamplingTrainProblemComponentExists = emParams.MatsSamplingTrainProblemComponentExists;
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Number of Unit Operating Days
        /// 
        /// Check the Number of Unit Operating Days During Sampling Period.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MatsTrp14(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {

                if (emParams.MatsSorbentTrapRecord.ModcCd != "34")
                {
                    if (emParams.MatsSorbentTrapDictionary[emParams.MatsSorbentTrapRecord.TrapId].OperatingDateList.Count > 15)
                    {
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
        /// Ensure that Active ST or CEMST Methods Span the Sorbent Trap Period.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrp15(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                DateTime trapBeginDateHour = emParams.MatsSorbentTrapRecord.BeginDatehour.Default(DateTime.MinValue);
                DateTime trapEndDateHour = emParams.MatsSorbentTrapRecord.EndDatehour.Default(DateTime.MaxValue);

                CheckDataView<VwMonitorMethodRow> methodView 
                    = emParams.MethodRecords.FindRows(new cFilterCondition("METHOD_CD", "ST,CEMST", eFilterConditionStringCompare.InList),
                                                          new cFilterCondition("BEGIN_DATEHOUR", eFilterConditionRelativeCompare.LessThanOrEqual, trapEndDateHour, eNullDateDefault.Min),
                                                          new cFilterCondition("END_DATEHOUR", eFilterConditionRelativeCompare.GreaterThanOrEqual, trapBeginDateHour, eNullDateDefault.Max));

                if (methodView.Count == 0)
                {
                    category.CheckCatalogResult = "A";
                }
                else if (!(CheckForHourRangeCovered(category, methodView.SourceView, trapBeginDateHour.Date, trapBeginDateHour.Hour, trapEndDateHour.Date, trapEndDateHour.Hour)))
                {
                    category.CheckCatalogResult = "B";
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