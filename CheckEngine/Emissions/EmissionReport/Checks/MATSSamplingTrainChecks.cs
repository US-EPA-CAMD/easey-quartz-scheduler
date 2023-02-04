using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cMATSSamplingTrainChecks : cEmissionsChecks
    {

        #region Constructors

        public cMATSSamplingTrainChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[18];

            CheckProcedures[1] = new dCheckProcedure(MatsTrn1);
            CheckProcedures[2] = new dCheckProcedure(MatsTrn2);
            CheckProcedures[3] = new dCheckProcedure(MatsTrn3);
            CheckProcedures[4] = new dCheckProcedure(MatsTrn4);
            CheckProcedures[5] = new dCheckProcedure(MatsTrn5);
            CheckProcedures[6] = new dCheckProcedure(MatsTrn6);
            CheckProcedures[7] = new dCheckProcedure(MatsTrn7);
            CheckProcedures[8] = new dCheckProcedure(MatsTrn8);
            CheckProcedures[9] = new dCheckProcedure(MatsTrn9);
            CheckProcedures[10] = new dCheckProcedure(MatsTrn10);
            CheckProcedures[11] = new dCheckProcedure(MatsTrn11);
            CheckProcedures[12] = new dCheckProcedure(MatsTrn12);
            CheckProcedures[13] = new dCheckProcedure(MatsTrn13);
            CheckProcedures[14] = new dCheckProcedure(MatsTrn14);
            CheckProcedures[15] = new dCheckProcedure(MatsTrn15);
            CheckProcedures[16] = new dCheckProcedure(MatsTrn16);
            CheckProcedures[17] = new dCheckProcedure(MatsTrn17);
        }

        #endregion


        #region Checks 1-10

        /// <summary>
        /// Component Id Valid
        /// 
        /// Ensure that Component ID exists Sampling Train, and that the type of the associated component is 'STRAIN'.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsSamplingTrainComponentIdValid = false;

                if (emParams.MatsSamplingTrainRecord.ComponentId == null)
                {
                   emParams.MatsSamplingTrainProblemComponentExists = true;
                    category.CheckCatalogResult = "A";
                }
                else if (emParams.MatsSamplingTrainRecord.ComponentTypeCd != "STRAIN")
                {
                   emParams.MatsSamplingTrainProblemComponentExists = true;
                    category.CheckCatalogResult = "B";
                }
                else
                {
                   emParams.MatsSamplingTrainComponentIdValid = true;

                    SamplingTrainEvalInformation samplingTrainEvalInformation = new SamplingTrainEvalInformation(emParams.MatsSamplingTrainRecord.TrapTrainId, 
                                                                                                                 (emParams.MatsSamplingTrainRecord.BorderTrapInd == 1), 
                                                                                                                 (emParams.MatsSamplingTrainRecord.SuppDataInd == 1));
                    {
                        if (emParams.MatsSamplingTrainRecord.SuppDataInd == 1)
                        {
                            /* Initialize all values for supplemental data, since they were previously checked and will not be set by checks in this evaluation. */
                            samplingTrainEvalInformation.HgConcentration =emParams.MatsSamplingTrainRecord.HgConcentration.ScientificNotationtoDecimal();
                            samplingTrainEvalInformation.TrainQAStatusCode =emParams.MatsSamplingTrainRecord.TrainQaStatusCd;
                            samplingTrainEvalInformation.ReferenceSFSRRatio =emParams.MatsSamplingTrainRecord.RefFlowToSamplingRatio;
                            samplingTrainEvalInformation.TotalSFSRRatioCount =emParams.MatsSamplingTrainRecord.SfsrTotalCount.Default(0);
                            samplingTrainEvalInformation.DeviatedSFSRRatioCount =emParams.MatsSamplingTrainRecord.SfsrDeviatedCount.Default(0);
                            samplingTrainEvalInformation.TotalGfmCount =emParams.MatsSamplingTrainRecord.GfmTotalCount.Default(0);
                            samplingTrainEvalInformation.NotAvailablelGfmCount =emParams.MatsSamplingTrainRecord.GfmNotAvailableCount.Default(0);
                        }
                        else
                        {
                            samplingTrainEvalInformation.HgConcentration = null;
                            samplingTrainEvalInformation.TrainQAStatusCode = null;
                            samplingTrainEvalInformation.ReferenceSFSRRatio = null;
                            samplingTrainEvalInformation.TotalSFSRRatioCount = 0;
                            samplingTrainEvalInformation.DeviatedSFSRRatioCount = 0;
                            samplingTrainEvalInformation.TotalGfmCount = 0;
                            samplingTrainEvalInformation.NotAvailablelGfmCount = 0;
                        }

                        samplingTrainEvalInformation.SamplingTrainValid = true;
                    }

                   emParams.MatsSamplingTrainDictionary[samplingTrainEvalInformation.TrapTrainId] = samplingTrainEvalInformation;
                   emParams.MatsSorbentTrapSamplingTrainList.Add(samplingTrainEvalInformation);
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Sorbent Trap Serial Number
        /// 
        /// Check that a sorbent trap serial number is provided.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn2(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainRecord.SorbentTrapSerialNumber == null)
                {
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
        /// Train Quality Assurance Status Valid
        /// 
        /// Check Sampling Train Quality Assurance Status Matches Lookup Table
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsSamplingTrainQaStatusCodeValid = false;

                if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd == null)
                {
                    if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                       emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                    category.CheckCatalogResult = "A";
                }
                else if (emParams.MatsSamplingTrainQaStatusLookupTable.CountRows(new cFilterCondition[] { new cFilterCondition("TRAIN_QA_STATUS_CD",emParams.MatsSamplingTrainRecord.TrainQaStatusCd) }) == 0)
                {
                    if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                       emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                    category.CheckCatalogResult = "B";
                }
                else
                {
                   emParams.MatsSamplingTrainQaStatusCodeValid = true;

                    if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                       emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].TrainQAStatusCode =emParams.MatsSamplingTrainRecord.TrainQaStatusCd;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Main Trap Hg Valid
        /// 
        /// Main Trap Hg Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsMainTrapHgValid = false;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.MainTrapHg == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsMainTrapHgValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSamplingTrainRecord.MainTrapHg.MatsSignificantDigitsValid(emParams.MatsSamplingTrainRecord.EndDatehour.Value))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                           emParams.MatsMainTrapHgValid = true;
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
        /// Breakthrough Trap Hg Valid
        /// 
        /// Breakthrough Trap Hg Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsBtTrapHgValid = false;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.BreakthroughTrapHg == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsBtTrapHgValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSamplingTrainRecord.BreakthroughTrapHg.MatsSignificantDigitsValid(emParams.MatsSamplingTrainRecord.EndDatehour.Value))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                           emParams.MatsBtTrapHgValid = true;
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
        /// Spike Trap Hg Valid
        /// 
        /// Spike Trap Hg Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn6(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsSpikeTrapHgValid = false;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.SpikeTrapHg == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsSpikeTrapHgValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSamplingTrainRecord.SpikeTrapHg.MatsSignificantDigitsValid(emParams.MatsSamplingTrainRecord.EndDatehour.Value))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                           emParams.MatsSpikeTrapHgValid = true;
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
        /// Spike Reference Value Valid
        /// 
        /// Spike Reference Value Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn7(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsSpikeReferenceValueValid = false;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.SpikeRefValue == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsSpikeReferenceValueValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSamplingTrainRecord.SpikeRefValue.MatsSignificantDigitsValid(emParams.MatsSamplingTrainRecord.EndDatehour.Value))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                           emParams.MatsSpikeReferenceValueValid = true;
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
        /// Total Sample Volume DSCM Valid
        /// 
        /// Total Sample Volume DSCM Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn8(cCategory category, ref bool log)
        {
            string returnVal = "";
            decimal dValue = 0.0M;
            try
            {
               emParams.MatsTotalSampleVolumeDscmValid = false;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.TotalSampleVolume == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsTotalSampleVolumeDscmValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        //  EC-2451 M. Jones 2015-11-05 
                        //  float type and regular expression pattern enforces at least two (2) digits and 
                        //  no more than four (4) following decimal.
                        else if (!decimal.TryParse(emParams.MatsSamplingTrainRecord.TotalSampleVolume.Value.ToString(),
                                                   out dValue))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                           emParams.MatsTotalSampleVolumeDscmValid = true;
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
        /// Sampling Ratio Check Result Code Valid
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn9(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.RefFlowToSamplingRatio == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST")  &&
                            (emParams.MatsSamplingTrainRecord.RataInd != 1))
                        {
                            if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "A";
                        }
                        else
                        {
                           emParams.MatsTotalSampleVolumeDscmValid = true;
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsSamplingTrainRecord.RefFlowToSamplingRatio.Value != Math.Round(emParams.MatsSamplingTrainRecord.RefFlowToSamplingRatio.Value, 2, MidpointRounding.AwayFromZero))
                        {
                            if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].ReferenceSFSRRatio =emParams.MatsSamplingTrainRecord.RefFlowToSamplingRatio;
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
        /// Total Sample Volume DSCM Valid
        /// 
        /// Total Sample Volume DSCM Null or Reported to Two Decimal Places.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn10(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.SamplingRatioTestResultCd == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.SamplingRatioTestResultCd == "PASSED")
                        {
                            if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                            {
                                if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                                   emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                                category.CheckCatalogResult = "B";
                            }
                        }
                        else if (emParams.MatsSamplingTrainRecord.SamplingRatioTestResultCd == "FAILED")
                        {
                            if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd != "FAILED")
                            {
                                if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                                   emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                                category.CheckCatalogResult = "C";
                            }
                        }
                        else
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "D";
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


        #region Checks 11-20

        /// <summary>
        /// Post Leak Check Result Code Valid.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn11(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.PostLeakTestResultCd == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.PostLeakTestResultCd == "PASSED")
                        {
                            if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                            {
                                category.CheckCatalogResult = "B";
                            }
                        }
                        else if (emParams.MatsSamplingTrainRecord.PostLeakTestResultCd == "FAILED")
                        {
                            if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd != "FAILED")
                            {
                                category.CheckCatalogResult = "C";
                            }
                        }
                        else
                        {
                            category.CheckCatalogResult = "D";
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
        /// Sample Damage Explanation
        /// 
        /// Sample Damage Explanation is provided if QA Status Code equals LOST.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn12(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.SampleDamageExplanation == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd == "LOST")
                        {
                            category.CheckCatalogResult = "A";
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
        /// Hg Concentration Reported Properly
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn13(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsCalcTrainHgConcentration = null;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.HgConcentration == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "B";
                        }
                        else if (!emParams.MatsSamplingTrainRecord.HgConcentration.MatsSignificantDigitsValid(emParams.MatsSamplingTrainRecord.EndDatehour.Value))
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                            category.CheckCatalogResult = "C";
                        }
                        else if (emParams.MatsSamplingTrainRecord.TrapModcCd.InList("43,44"))
                        {
                           emParams.MatsCalcTrainHgConcentration =emParams.MatsSamplingTrainRecord.HgConcentration;

                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].HgConcentration =emParams.MatsSamplingTrainRecord.HgConcentration.ScientificNotationtoDecimal();
                        }
                        else if (emParams.MatsMainTrapHgValid.Default(false) &&emParams.MatsBtTrapHgValid.Default(false) &&emParams.MatsTotalSampleVolumeDscmValid.Default(false) &&emParams.MatsSamplingTrainRecord.TotalSampleVolume != null &&emParams.MatsSamplingTrainRecord.TotalSampleVolume != 0)
                        {
                            decimal hgConcentration
                              = ((emParams.MatsSamplingTrainRecord.MainTrapHg.ScientificNotationtoDecimal() +emParams.MatsSamplingTrainRecord.BreakthroughTrapHg.ScientificNotationtoDecimal())
                                 /emParams.MatsSamplingTrainRecord.TotalSampleVolume.Value);

                           emParams.MatsCalcTrainHgConcentration = hgConcentration.MatsSignificantDigitsFormat(emParams.MatsSamplingTrainRecord.EndDatehour.Value,
                                                                                                                   emParams.MatsSamplingTrainRecord.HgConcentration);

                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].HgConcentration =emParams.MatsCalcTrainHgConcentration.ScientificNotationtoDecimal();

                            if (emParams.MatsSamplingTrainRecord.HgConcentration !=emParams.MatsCalcTrainHgConcentration)
                            {
                                if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                                   emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;

                                category.CheckCatalogResult = "D";
                            }
                        }
                        else // Calculation input is not valid
                        {
                            if (emParams.MatsSamplingTrainComponentIdValid.Default(false))
                               emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId].SamplingTrainValid = false;
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
        /// Percent Breakthrough Reported Properly
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn14(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
               emParams.MatsCalcTrainPercentBreakthrough = null;

                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.PercentBreakthrough == null)
                    {
                        if ((emParams.MatsSamplingTrainRecord.TrainQaStatusCd.InList("PASSED,FAILED,UNCERTAIN")) &&
                            (emParams.MatsSamplingTrainRecord.HgConcentration.ScientificNotationtoDecimal() >= 0.2m))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.InList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsSamplingTrainRecord.PercentBreakthrough != Math.Round(emParams.MatsSamplingTrainRecord.PercentBreakthrough.Value, 1, MidpointRounding.AwayFromZero))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else if (emParams.MatsMainTrapHgValid.Default(false) &&emParams.MatsBtTrapHgValid.Default(false) &&emParams.MatsSamplingTrainRecord.MainTrapHg != null &&emParams.MatsSamplingTrainRecord.MainTrapHg.ScientificNotationtoDecimal() != 0)
                        {
                           emParams.MatsCalcTrainPercentBreakthrough = Math.Round((emParams.MatsSamplingTrainRecord.BreakthroughTrapHg.ScientificNotationtoDecimal() /emParams.MatsSamplingTrainRecord.MainTrapHg.ScientificNotationtoDecimal()) * 100, 1, MidpointRounding.AwayFromZero);

                            if (emParams.MatsSamplingTrainRecord.PercentBreakthrough !=emParams.MatsCalcTrainPercentBreakthrough)
                            {
                                category.CheckCatalogResult = "D";
                            }
                            else
                            {
                                decimal hgConcentration =emParams.MatsSamplingTrainRecord.HgConcentration.ScientificNotationtoDecimal();
                                decimal roundedPecentBreakthrough = Math.Round(emParams.MatsSamplingTrainRecord.PercentBreakthrough.Value, 0, MidpointRounding.AwayFromZero);

                                /* RATA APS*/
                                if (emParams.MatsSamplingTrainRecord.SorbentTrapApsCd == "RATA")
                                {
                                    if (((hgConcentration > 1m) && (roundedPecentBreakthrough > 10)) ||
                                        ((hgConcentration > 0.5m) && (roundedPecentBreakthrough > 20)) ||
                                        ((hgConcentration > 0.1m) && (roundedPecentBreakthrough > 50)))
                                    {
                                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd != "FAILED")
                                        {
                                            category.CheckCatalogResult = "F";
                                        }
                                    }
                                }

                                /* No APS,  PS-12B */
                                else
                                {
                                    if (hgConcentration >= 0.2m)
                                    {
                                        if ((roundedPecentBreakthrough > 10) || ((roundedPecentBreakthrough > 5) && (hgConcentration > 0.5m)))
                                        {
                                            if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd != "FAILED")
                                            {
                                                category.CheckCatalogResult = "E";
                                            }
                                        }
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

        /// <summary>
        /// Percent Spike Recovery Reported Properly
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn15(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainQaStatusCodeValid.Default(false))
                {
                    if (emParams.MatsSamplingTrainRecord.PercentSpikeRecovery == null)
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("INC,EXPIRED,LOST"))
                        {
                            category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.NotInList("PASSED,FAILED,UNCERTAIN"))
                        {
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsSamplingTrainRecord.PercentSpikeRecovery != Math.Round(emParams.MatsSamplingTrainRecord.PercentSpikeRecovery.Value, 1, MidpointRounding.AwayFromZero))
                        {
                            category.CheckCatalogResult = "C";
                        }
                        else if (emParams.MatsSpikeTrapHgValid.Default(false) &&emParams.MatsSpikeReferenceValueValid.Default(false) &&emParams.MatsSamplingTrainRecord.SpikeRefValue != null &&emParams.MatsSamplingTrainRecord.SpikeRefValue.ScientificNotationtoDecimal() != 0)
                        {
                           emParams.MatsCalcTrainPercentSpikeRecovery = Math.Round((emParams.MatsSamplingTrainRecord.SpikeTrapHg.ScientificNotationtoDecimal() /emParams.MatsSamplingTrainRecord.SpikeRefValue.ScientificNotationtoDecimal()) * 100, 1, MidpointRounding.AwayFromZero);

                            if (emParams.MatsSamplingTrainRecord.PercentSpikeRecovery !=emParams.MatsCalcTrainPercentSpikeRecovery)
                            {
                                category.CheckCatalogResult = "D";
                            }
                            else if ((emParams.MatsSamplingTrainRecord.PercentSpikeRecovery < 75) || (emParams.MatsSamplingTrainRecord.PercentSpikeRecovery > 125))
                            {
                                if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd != "FAILED")
                                {
                                    category.CheckCatalogResult = "E";
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
        /// Check Hourly Sampling Ratios
        /// 
        /// Compare Hourly Sampling Ratio with PS12B Requirement
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn16(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                SamplingTrainEvalInformation samplingTrainEvalInformation = null;
                bool? sorbentTrapValid = false;
                if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                {
                    samplingTrainEvalInformation =emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId];
                    {
                        sorbentTrapValid = samplingTrainEvalInformation.SamplingTrainValid;
                    }
                }

                if (sorbentTrapValid.Default(false))
                {
                    int totalSfsrRatioCount = samplingTrainEvalInformation.TotalSFSRRatioCount;
                    int deviatedSfsrRatioCount = samplingTrainEvalInformation.DeviatedSFSRRatioCount;
                    bool samplingTrainCountsAreComplete = (samplingTrainEvalInformation.IsBorderTrain == false) || (samplingTrainEvalInformation.IsSupplementalData == true);

                    if (totalSfsrRatioCount >= 100)
                    {
                        decimal matsCalcPercentSfsrRatioDev = Math.Round(((decimal)deviatedSfsrRatioCount / (decimal)totalSfsrRatioCount) * 100, 0, MidpointRounding.AwayFromZero);

                        if (emParams.MatsSamplingTrainRecord.SamplingRatioTestResultCd == "PASSED")
                        {
                            if (matsCalcPercentSfsrRatioDev > 5)
                                category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        if (emParams.MatsSamplingTrainRecord.SamplingRatioTestResultCd == "PASSED")
                        {
                            if (deviatedSfsrRatioCount > 5)
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
        /// Checks the percentage of GFM with an "N" (Not Allowed) Begin End Flag.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MatsTrn17(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.MatsSamplingTrainRecord.TrainQaStatusCd.InList("PASSED,FAILED,UNCERTAIN") && (emParams.MatsSamplingTrainRecord.RataInd.Default(0) == 0))
                {
                    if (emParams.MatsSamplingTrainDictionary.ContainsKey(emParams.MatsSamplingTrainRecord.TrapTrainId))
                    {
                        SamplingTrainEvalInformation dictionaryEntry =emParams.MatsSamplingTrainDictionary[emParams.MatsSamplingTrainRecord.TrapTrainId];
                        {
                            if (dictionaryEntry.SamplingTrainValid == true)
                            {
                                if ((dictionaryEntry.TotalGfmCount > 0) && (dictionaryEntry.NotAvailablelGfmCount >= 0))
                                {
                                    decimal notAvailableGfmPercent = 100 * dictionaryEntry.NotAvailablelGfmCount / dictionaryEntry.TotalGfmCount;

                                    if (notAvailableGfmPercent >= 20m)
                                    {
                                        category.CheckCatalogResult = "A";
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