using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cMATSCalculatedHourlyValueChecks : cEmissionsChecks
    {

        #region Constructors

        public cMATSCalculatedHourlyValueChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[21];

            CheckProcedures[1] = new dCheckProcedure(MATSCHV1);
            CheckProcedures[2] = new dCheckProcedure(MATSCHV2);
            CheckProcedures[3] = new dCheckProcedure(MATSCHV3);
            CheckProcedures[4] = new dCheckProcedure(MATSCHV4);
            CheckProcedures[5] = new dCheckProcedure(MATSCHV5);
            CheckProcedures[6] = new dCheckProcedure(MATSCHV6);
            CheckProcedures[7] = new dCheckProcedure(MATSCHV7);
            CheckProcedures[8] = new dCheckProcedure(MATSCHV8);
            CheckProcedures[9] = new dCheckProcedure(MATSCHV9);
            CheckProcedures[10] = new dCheckProcedure(MATSCHV10);
            CheckProcedures[11] = new dCheckProcedure(MATSCHV11);
            CheckProcedures[12] = new dCheckProcedure(MATSCHV12);
            CheckProcedures[13] = new dCheckProcedure(MATSCHV13);
            CheckProcedures[14] = new dCheckProcedure(MATSCHV14);
            CheckProcedures[15] = new dCheckProcedure(MATSCHV15);
            CheckProcedures[16] = new dCheckProcedure(MATSCHV16);
            CheckProcedures[17] = new dCheckProcedure(MATSCHV17);
            CheckProcedures[18] = new dCheckProcedure(MATSCHV18);
            CheckProcedures[19] = new dCheckProcedure(MATSCHV19);
            CheckProcedures[20] = new dCheckProcedure(MATSCHV20);
        }

        #endregion

        #region Checks 1-10

        public  string MATSCHV1(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HGRE.
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("6.24E-11");
                emParams.CurrentDhvParameter = emParams.MatsHgDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHgDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHgDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHgcValue;
                emParams.MatsMhvRecord = emParams.MatsHgcMhvRecord;
                emParams.MatsMoistureEquationList = "A-3";

                emParams.MatsDhvMeasuredModcList = "36,39";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = (emParams.CurrentHourlyOpRecord.MatsHourLoad.Default(0m) != 0)
                                                   ? 1000 / emParams.CurrentHourlyOpRecord.MatsHourLoad.Value
                                                   : (decimal?) null;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV1");
            }

            return ReturnVal;
        }

        public  string MATSCHV2(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HCLRE. 
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("9.43E-8");
                emParams.CurrentDhvParameter = emParams.MatsHclDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHclDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHclDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHclcValue;
                emParams.MatsMhvRecord = emParams.MatsHclcMhvRecord;
                emParams.MatsMoistureEquationList = "HC-3";

                emParams.MatsDhvMeasuredModcList = "36,39";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = (emParams.CurrentHourlyOpRecord.MatsHourLoad.Default(0m) != 0)
                                                   ? 1 / emParams.CurrentHourlyOpRecord.MatsHourLoad.Value
                                                   : (decimal?)null;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV2");
            }

            return ReturnVal;
        }

        public  string MATSCHV3(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HFRE.
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("5.18E-8");
                emParams.CurrentDhvParameter = emParams.MatsHfDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHfDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHfDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHfcValue;
                emParams.MatsMhvRecord = emParams.MatsHfcMhvRecord;
                emParams.MatsMoistureEquationList = "HF-3";

                emParams.MatsDhvMeasuredModcList = "36,39";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = (emParams.CurrentHourlyOpRecord.MatsHourLoad.Default(0m) != 0)
                                                   ? 1 / emParams.CurrentHourlyOpRecord.MatsHourLoad.Value
                                                   : (decimal?)null;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV3");
            }

            return ReturnVal;
        }

        public  string MATSCHV4(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for SO2RE.
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("1.66E-7");
                emParams.CurrentDhvParameter = emParams.MatsSo2DhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsSo2DhvValid;
                emParams.MatsDhvRecord = emParams.MatsSo2DhvRecord;
                emParams.MatsMoistureEquationList = "S-3";

                emParams.MatsDhvMeasuredModcList = "36,39";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = (emParams.CurrentHourlyOpRecord.MatsHourLoad.Default(0m) != 0)
                                                   ? 1 / emParams.CurrentHourlyOpRecord.MatsHourLoad.Value
                                                   : (decimal?)null;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV4");
            }

            return ReturnVal;
        }

        public  string MATSCHV5(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HGRH. 
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("6.24E-11");
                emParams.CurrentDhvParameter = emParams.MatsHgDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHgDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHgDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHgcValue;
                emParams.MatsMhvRecord = emParams.MatsHgcMhvRecord;
                emParams.MatsMoistureEquationList = "19-3,19-3D,19-4,19-5,19-8,19-9";

                emParams.MatsDhvMeasuredModcList = "36,37";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = 1000000m;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV5");
            }

            return ReturnVal;
        }

        public  string MATSCHV6(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HCLRH. 
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("9.43E-8");
                emParams.CurrentDhvParameter = emParams.MatsHclDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHclDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHclDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHclcValue;
                emParams.MatsMhvRecord = emParams.MatsHclcMhvRecord;
                emParams.MatsMoistureEquationList = "19-3,19-3D,19-4,19-5,19-8,19-9";

                emParams.MatsDhvMeasuredModcList = "36,37";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = 1m;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV6");
            }

            return ReturnVal;
        }

        public  string MATSCHV7(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for HFRH. 
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("5.18E-8");
                emParams.CurrentDhvParameter = emParams.MatsHfDhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsHfDhvValid;
                emParams.MatsDhvRecord = emParams.MatsHfDhvRecord;
                emParams.MatsMhvCalculatedValue = emParams.MatsMhvCalculatedHfcValue;
                emParams.MatsMhvRecord = emParams.MatsHfcMhvRecord;
                emParams.MatsMoistureEquationList = "19-3,19-3D,19-4,19-5,19-8,19-9";

                emParams.MatsDhvMeasuredModcList = "36,37";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = 1m;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV7");
            }

            return ReturnVal;
        }

        public  string MATSCHV8(cCategory Category, ref bool Log)
        //This check sets generic parameters and output parameters for subsequent Calculated hourly checks for SO2RH. 
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConversionFactor = ECMPS.Definitions.Extensions.cExtensions.ScientificNotationtoDecimal("1.66E-7");
                emParams.CurrentDhvParameter = emParams.MatsSo2DhvParameter;
                emParams.CurrentDhvRecordValid = emParams.MatsSo2DhvValid;
                emParams.MatsDhvRecord = emParams.MatsSo2DhvRecord;
                emParams.MatsMoistureEquationList = "19-3,19-3D,19-4,19-5,19-8,19-9";

                emParams.MatsDhvMeasuredModcList = "36,37";
                emParams.MatsDhvUnavailableModcList = "38";

                emParams.FinalConversionFactor = 1m;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV8");
            }

            return ReturnVal;
        }

        #region MATSCHV9
        public  string MATSCHV9(cCategory Category, ref bool Log)
        //Determines the main concentration value to use in calculations.
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConcentration = null;
                emParams.CalculationConcentrationSubstituted = false;

                if ((bool)emParams.CurrentDhvRecordValid && (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList.ToString())))
                {
                    if (emParams.MatsMhvCalculatedValue != null)
                    {
                        emParams.CalculationConcentration = emParams.MatsMhvCalculatedValue.ScientificNotationtoDecimal();
                    }
                    if ((emParams.MatsMhvRecord != null) && emParams.MatsMhvRecord.ModcCd.InList("34,35"))
                    {
                        emParams.CalculationConcentrationSubstituted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV9");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSCHV10
        public  string MATSCHV10(cCategory Category, ref bool Log)
        //Determines the SO2 concentration value to use in calculations.
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationConcentration = null;
                emParams.CalculationConcentrationSubstituted = false;

                if ((bool)emParams.CurrentDhvRecordValid && (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList.ToString())))
                {
                    if (emParams.CurrentSo2MonitorHourlyRecord != null)
                    {
                        emParams.CalculationConcentration = emParams.CurrentSo2MonitorHourlyRecord.UnadjustedHrlyValue;

                        if (emParams.CurrentSo2MonitorHourlyRecord.ModcCd.InList("05,06,07,08,09,10,12,13,15,18,23,55"))
                        {
                            emParams.CalculationConcentrationSubstituted = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV10");
            }

            return ReturnVal;
        }
        #endregion

        #endregion

        #region Checks 11-20

        #region MATSCHV11
        public  string MATSCHV11(cCategory Category, ref bool Log)
        //
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationFlow = null;
                emParams.CalculationFlowSubstituted = false;

                if (emParams.CurrentDhvRecordValid.Default(false) && (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList.ToString())))
                {
                    if (emParams.CurrentFlowMonitorHourlyRecord != null)
                    {
                        emParams.CalculationFlow = emParams.CurrentFlowMonitorHourlyRecord.UnadjustedHrlyValue;

                        if (emParams.CurrentFlowMonitorHourlyRecord.ModcCd.NotInList("01,02,03,04,20,53,54"))
                        {
                            emParams.CalculationFlowSubstituted = true;
                        }
                    }
                    else
                    {
                        emParams.CalculationFlow = null;
                        emParams.CalculationFlowSubstituted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV11");
            }

            return ReturnVal;
        }
        #endregion

        /// <summary>
        /// MatsChv-12:
        /// 
        /// Determine the Calculation Diluent Value
        /// </summary>
        /// <param name="Category">Category object for category in which the check is running.</param>
        /// <param name="Log">Obsolete flag indicating whether to update the log.</param>
        /// <returns></returns>
        public  string MATSCHV12(cCategory Category, ref bool Log)
        //
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationDiluent = null;
                emParams.CalculationDiluentSubstituted = false;

                if ((bool)emParams.CurrentDhvRecordValid && (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList.ToString())))
                {
                    if (emParams.MatsDhvRecord.EquationCd.InList("19-3D,19-5D") || emParams.MatsDhvRecord.ModcCd == "37")
                    {
                        if (emParams.MatsDhvRecord.EquationCd.InList("19-1,19-2,19-3,19-3D,19-4,19-5,19-5D"))
                        {
                            DataView O2MonitorDefaultRecords = cRowFilter.FindRows(emParams.MonitorDefaultRecordsByHourLocation.SourceView,
                                new cFilterCondition[] {
                                new cFilterCondition("PARAMETER_CD", "O2X"),
                                new cFilterCondition("DEFAULT_PURPOSE_CD", "DC"),
                                new cFilterCondition("FUEL_CD", "NFS")
                            });
                            int O2MonitorDefaultMatches = O2MonitorDefaultRecords.Count;

                            if (O2MonitorDefaultMatches > 1)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                            else if (O2MonitorDefaultMatches == 0)
                            {
                                Category.CheckCatalogResult = "B";
                            }
                            else
                            {
                                DataRowView O2MonitorDefaultRecord = O2MonitorDefaultRecords[0];
                                if (O2MonitorDefaultRecord["DEFAULT_VALUE"] == null || O2MonitorDefaultRecord["DEFAULT_VALUE"].AsDecimal() <= 0)
                                {
                                    Category.CheckCatalogResult = "C";
                                }
                                else
                                {
                                    emParams.CalculationDiluent = O2MonitorDefaultRecord["DEFAULT_VALUE"].AsDecimal();
                                }
                            }
                        }
                        else if (emParams.MatsDhvRecord.EquationCd.InList("19-6,19-7,19-8,19-9"))
                        {
                            DataView Co2MonitorDefaultRecords = cRowFilter.FindRows(emParams.MonitorDefaultRecordsByHourLocation.SourceView,
                                new cFilterCondition[] {
                                new cFilterCondition("PARAMETER_CD", "CO2N"),
                                new cFilterCondition("DEFAULT_PURPOSE_CD", "DC"),
                                new cFilterCondition("FUEL_CD", "NFS")
                            });
                            int Co2MonitorDefaultMatches = Co2MonitorDefaultRecords.Count;
                            if (Co2MonitorDefaultMatches > 1)
                            {
                                Category.CheckCatalogResult = "D";
                            }
                            else if (Co2MonitorDefaultMatches == 0)
                            {
                                Category.CheckCatalogResult = "E";
                            }
                            else
                            {
                                DataRowView Co2MonitorDefaultRecord = Co2MonitorDefaultRecords[0];
                                if (Co2MonitorDefaultRecord["DEFAULT_VALUE"] == null || Co2MonitorDefaultRecord["DEFAULT_VALUE"].AsDecimal() <= 0)
                                {
                                    Category.CheckCatalogResult = "F";
                                }
                                else
                                {
                                    emParams.CalculationDiluent = Co2MonitorDefaultRecord["DEFAULT_VALUE"].AsDecimal();
                                }
                            }
                        }
                    }

                    else //not Equation 19-3D,19-5D or Modc 14
                        if (emParams.MatsDhvRecord.EquationCd.InList("19-1,19-4") && emParams.O2DryNeededForMats == true)
                    {
                        emParams.CalculationDiluent = emParams.O2DryCalculatedAdjustedValue;
                        if (emParams.O2DryModc.NotInList("01,02,03,04,17,20,53,54"))
                        {
                            emParams.CalculationDiluentSubstituted = true;
                        }
                    }
                    else if (emParams.MatsDhvRecord.EquationCd.InList("19-2,19-3,19-5") && emParams.O2WetNeededForMats == true)
                    {
                        emParams.CalculationDiluent = emParams.O2WetCalculatedAdjustedValue;

                        if (emParams.O2WetModc.NotInList("01,02,03,04,17,20,53,54"))
                        {
                            emParams.CalculationDiluentSubstituted = true;
                        }
                    }
                    else if (emParams.MatsDhvRecord.EquationCd.InList("19-6,19-7,19-8,19-9") && emParams.Co2DiluentNeededForMats == true)
                    {
                        emParams.CalculationDiluent = emParams.Co2cMhvCalculatedAdjustedValue;
                        if (emParams.Co2cMhvModc.NotInList("01,02,03,04,17,20,21,53,54"))
                        {
                            emParams.CalculationDiluentSubstituted = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV12");
            }

            return ReturnVal;
        }

        #region MATSCHV13
        public  string MATSCHV13(cCategory Category, ref bool Log)
        //
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculationMoisture = null;
                emParams.CalculationMoistureSubstituted = false;

                if ((bool)emParams.CurrentDhvRecordValid && emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList.ToString()))
                {
                    if (emParams.MatsDhvRecord.EquationCd.InList(emParams.MatsMoistureEquationList.ToString()))
                    {
                        if ((emParams.H2oMethodCode == "MWD") && (bool)emParams.H2oDerivedHourlyChecksNeeded && (emParams.H2oDhvCalculatedAdjustedValue != null))
                        {
                            emParams.CalculationMoisture = emParams.H2oDhvCalculatedAdjustedValue;

                            if (emParams.H2oDhvModc.NotInList("01,02,03,04,05,06,07,08,09,10,12,21,53,54,55"))
                            {
                                emParams.CalculationMoistureSubstituted = true;
                            }
                        }
                        else if (emParams.H2oMethodCode.InList("MMS,MTB") && (bool)emParams.H2oMonitorHourlyChecksNeeded && emParams.H2oMhvCalculatedAdjustedValue != null)
                        {
                            emParams.CalculationMoisture = emParams.H2oMhvCalculatedAdjustedValue;

                            if (emParams.H2oMhvModc.NotInList("01,02,03,04,06,07,08,09,10,12,21,53,54,55"))
                            {
                                emParams.CalculationMoistureSubstituted = true;
                            }
                        }
                        else if (emParams.H2oMethodCode == "MDF" && (bool)emParams.H2oDerivedHourlyChecksNeeded && emParams.H2oDhvCalculatedAdjustedValue != null)
                        {
                            emParams.CalculationMoisture = emParams.H2oDhvCalculatedAdjustedValue;

                            if (emParams.H2oDhvModc.NotInList("01,02,03,04,05,06,07,08,09,10,12,21,53,54,55"))
                            {
                                emParams.CalculationMoistureSubstituted = true;
                            }
                        }
                        else if (emParams.H2oMethodCode == "MDF" && (bool)emParams.H2oDerivedHourlyChecksNeeded == false && emParams.H2oDefaultValue != null)
                        {
                            emParams.CalculationMoisture = emParams.H2oDefaultValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV13");
            }

            return ReturnVal;
        }
        #endregion

        /// <summary>
        /// MatsChv-14: Check MODC and determine the MATS Formula Calculated Unadjusted Value
        /// </summary>
        /// <param name="Category">Check category in which the check is running.</param>
        /// <param name="Log">Obsolete parameter.</param>
        /// <returns>Returns the exception message if the check fails to run.</returns>
        public  string MATSCHV14(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculatedUnadjustedValue = null;

                if (emParams.CurrentDhvRecordValid.Default(false))
                {
                    /* Measured MODC */
                    if (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList))
                    {
                        if (emParams.MatsDhvRecord.EquationCd != null)
                        {
                            /* Moisture Equation */
                            if (emParams.MatsDhvRecord.EquationCd.InList(emParams.MatsMoistureEquationList))
                            {
                                /* An equation input was substituted */
                                if (emParams.CalculationConcentrationSubstituted.Default(false) || emParams.CalculationFlowSubstituted.Default(false) || emParams.CalculationMoistureSubstituted.Default(false))
                                {
                                    Category.CheckCatalogResult = "A";
                                }

                                /* An equation input is null */
                                else if ((emParams.CalculationConcentration == null) || (emParams.CalculationFlow == null) || (emParams.CalculationMoisture == null))
                                {
                                    Category.CheckCatalogResult = "B";
                                }

                                /* Calculate that baby! */
                                else if (emParams.FinalConversionFactor.HasValue)
                                {
                                    emParams.CalculatedUnadjustedValue = emParams.CalculationConversionFactor.Value
                                                                           * emParams.CalculationConcentration.Value
                                                                           * emParams.CalculationFlow.Value
                                                                           * (1m - emParams.CalculationMoisture.Value / 100)
                                                                           * emParams.FinalConversionFactor.Value;
                                }
                            }

                            /* Non Moisture Equation */
                            else
                            {
                                /* An equation input was substituted */
                                if (emParams.CalculationConcentrationSubstituted.Default(false) || emParams.CalculationFlowSubstituted.Default(false))
                                {
                                    Category.CheckCatalogResult = "C";
                                }

                                /* An equation input is null */
                                else if ((emParams.CalculationConcentration == null) || (emParams.CalculationFlow == null))
                                {
                                    Category.CheckCatalogResult = "D";
                                }

                                /* Calculate that baby! */
                                else if (emParams.FinalConversionFactor.HasValue)
                                {
                                    emParams.CalculatedUnadjustedValue = emParams.CalculationConversionFactor.Value
                                                                           * emParams.CalculationConcentration.Value
                                                                           * emParams.CalculationFlow.Value
                                                                           * emParams.FinalConversionFactor.Value;
                                }
                            }
                        }
                        else
                        {
                            Category.CheckCatalogResult = "E";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV14");
            }

            return ReturnVal;
        }

        /// <summary>
        /// MatsChv-15: Check MODC and determine the Formula 19 Calculated Unadjusted Value
        /// </summary>
        /// <param name="Category">Check category in which the check is running.</param>
        /// <param name="Log">Obsolete parameter.</param>
        /// <returns>Returns the exception message if the check fails to run.</returns>
        public  string MATSCHV15(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.CalculatedUnadjustedValue = null;

                if (emParams.CurrentDhvRecordValid.Default(false))
                {
                    /* Measured MODC */
                    if (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList))
                    {
                        if (emParams.MatsDhvRecord.EquationCd != null)
                        {
                            switch (emParams.MatsDhvRecord.EquationCd)
                            {

                                case "19-1":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)20.9)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor * ((decimal)20.9 / ((decimal)20.9 - emParams.CalculationDiluent))) * emParams.FinalConversionFactor.Value;
                                        }
                                        break;
                                    }
                                case "19-2":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else
                                        {
                                            decimal? MoistureFraction = null;
                                            DataView BwaDefaultRecords = cRowFilter.FindRows(emParams.MonitorDefaultRecordsByHourLocation.SourceView,
                                                                                                              new cFilterCondition[] {
                                                                                        new cFilterCondition("PARAMETER_CD", "BWA")
                                                                                                          });

                                            int BwaDefaultRecordCount = BwaDefaultRecords.Count;

                                            if (BwaDefaultRecordCount == 0)
                                            {
                                                MoistureFraction = (decimal)0.027;
                                            }

                                            else if (BwaDefaultRecordCount == 1)
                                            {
                                                decimal DefaultValue = (decimal)BwaDefaultRecords[0]["DEFAULT_VALUE"];
                                                if (DefaultValue > (decimal)0 && DefaultValue < (decimal)1)
                                                {
                                                    MoistureFraction = DefaultValue;
                                                }
                                            }
                                            else
                                            {
                                                Category.CheckCatalogResult = "F";
                                            }

                                            if (Category.CheckCatalogResult == null)
                                            {
                                                if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFwFactorExists.Default(false) == false || MoistureFraction == null)
                                                {
                                                    Category.CheckCatalogResult = "C";
                                                }
                                                else if (emParams.CalculationDiluent == (decimal)20.9 * (1 - MoistureFraction))
                                                {
                                                    Category.CheckCatalogResult = "D";
                                                }
                                                else if (emParams.FinalConversionFactor.HasValue)
                                                {
                                                    emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FwFactor * ((decimal)20.9 / ((decimal)20.9 * (1 - MoistureFraction) - emParams.CalculationDiluent))) * emParams.FinalConversionFactor.Value;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                case "19-3":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)20.9 * ((100 - emParams.CalculationMoisture) / 100))
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            decimal h2oFactor = ((decimal)100 - (decimal)emParams.CalculationMoisture) / (decimal)100.0;
                                            decimal denom = ((decimal)20.9 * h2oFactor) - (decimal)emParams.CalculationDiluent;

                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor * ((decimal)20.9 / denom)) * emParams.FinalConversionFactor.Value;
                                        }

                                        break;
                                    }

                                case "19-3D":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }

                                        else if (emParams.CalculationDiluent == (decimal)20.9)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }

                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            decimal h2oFactor = ((decimal)100 - (decimal)emParams.CalculationMoisture) / (decimal)100.0;
                                            decimal denom = ((decimal)20.9 * h2oFactor) - ((decimal)emParams.CalculationDiluent * h2oFactor);

                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor * ((decimal)20.9 / denom)) * emParams.FinalConversionFactor.Value;
                                        }

                                        break;
                                    }

                                case "19-4":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)20.9 || emParams.CalculationMoisture == 100)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * ((emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor) / ((100 - emParams.CalculationMoisture) / (decimal)100.0)) * ((decimal)20.9 / ((decimal)20.9 - emParams.CalculationDiluent))) * emParams.FinalConversionFactor.Value;
                                        }

                                        break;
                                    }
                                case "19-5":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)20.9 || emParams.CalculationMoisture == 100)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }

                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            decimal h2oFactor = (100 - (decimal)emParams.CalculationMoisture) / (decimal)100.0;
                                            decimal denom = (decimal)20.9 - (decimal)emParams.CalculationDiluent / h2oFactor;

                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor * (decimal)20.9 / denom) * emParams.FinalConversionFactor.Value;
                                        }


                                        break;
                                    }

                                case "19-5D":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFdFactorExists.Default(false) == false)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)20.9)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FdFactor * ((decimal)20.9 / ((decimal)20.9 - emParams.CalculationDiluent))) * emParams.FinalConversionFactor.Value;
                                        }

                                        break;
                                    }

                                case "19-6":
                                case "19-7":
                                case "":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFcFactorExists.Default(false) == false)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }

                                        else if (emParams.CalculationDiluent == (decimal)0.0)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }

                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FcFactor * ((decimal)100.0 / emParams.CalculationDiluent)) * emParams.FinalConversionFactor.Value;
                                        }
                                        break;
                                    }

                                case "19-8":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFcFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)0.0 || emParams.CalculationMoisture == 100)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * ((emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FcFactor) / (((decimal)100 - emParams.CalculationMoisture) / (decimal)100.0)) * ((decimal)100.0 / emParams.CalculationDiluent)) * emParams.FinalConversionFactor.Value;
                                        }
                                        break;
                                    }

                                case "19-9":
                                    {
                                        if (emParams.CalculationConcentrationSubstituted.Default(true) || emParams.CalculationDiluentSubstituted.Default(true) || emParams.CalculationMoistureSubstituted.Default(true))
                                        {
                                            Category.CheckCatalogResult = "A";
                                        }
                                        else if (emParams.CalculationDiluent == null || emParams.CalculationConcentration == null || emParams.ValidFcFactorExists.Default(false) == false || emParams.CalculationMoisture == null)
                                        {
                                            Category.CheckCatalogResult = "C";
                                        }
                                        else if (emParams.CalculationDiluent == (decimal)0.0)
                                        {
                                            Category.CheckCatalogResult = "D";
                                        }
                                        else if (emParams.FinalConversionFactor.HasValue)
                                        {
                                            decimal h2oFactor = (100 - (decimal)emParams.CalculationMoisture) / (decimal)100.0;
                                            decimal co2Term = (decimal)100.0 / (decimal)emParams.CalculationDiluent;

                                            emParams.CalculatedUnadjustedValue = (emParams.CalculationConversionFactor * emParams.CalculationConcentration * emParams.CurrentHourlyOpRecord.FcFactor * h2oFactor * co2Term) * emParams.FinalConversionFactor.Value;
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            Category.CheckCatalogResult = "B";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV15");
            }

            return ReturnVal;
        }

        /// <summary>
        /// Stores the HGRE or HGRH Calculated Unadjusted Value in the appropriate check parameters.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSCHV16(cCategory Category, ref bool Log)
        
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsCalculatedHgRateValue = emParams.CalculatedUnadjustedValue.MatsSignificantDigitsFormat(emParams.CurrentOperatingDate.Value, emParams.MatsDhvRecord.UnadjustedHrlyValue);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV16");
            }

            return ReturnVal;
        }



        /// <summary>
        /// Stores the HCLRE or HCLRH Calculated Unadjusted Value in the appropriate check parameters.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSCHV17(cCategory Category, ref bool Log)
        
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsCalculatedHclRateValue = emParams.CalculatedUnadjustedValue.MatsSignificantDigitsFormat(emParams.CurrentOperatingDate.Value, emParams.MatsDhvRecord.UnadjustedHrlyValue);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV17");
            }

            return ReturnVal;
        }


        /// <summary>
        /// Stores the HFRE or HFRH Calculated Unadjusted Value in the appropriate check parameters.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSCHV18(cCategory Category, ref bool Log)
        
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsCalculatedHfRateValue = emParams.CalculatedUnadjustedValue.MatsSignificantDigitsFormat(emParams.CurrentOperatingDate.Value, emParams.MatsDhvRecord.UnadjustedHrlyValue);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV18");
            }

            return ReturnVal;
        }


        /// <summary>
        /// Stores the SO2RE or SO2RH Calculated Unadjusted Value in the appropriate check parameters.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSCHV19(cCategory Category, ref bool Log)
        
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsCalculatedSo2RateValue = emParams.CalculatedUnadjustedValue.MatsSignificantDigitsFormat(emParams.CurrentOperatingDate.Value, emParams.MatsDhvRecord.UnadjustedHrlyValue);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSCHV19");
            }

            return ReturnVal;
        }


        /// <summary>
        /// MATSCHV-20: Check Unadjusted Hourly Value Tolerance
        /// 
        /// Determines whether the reported and calculated MATS rate values agree within a 5% difference.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string MATSCHV20(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {

                if (emParams.CurrentDhvRecordValid.Default(false))
                {
                    if (emParams.MatsDhvRecord.ModcCd.InList(emParams.MatsDhvMeasuredModcList)
                        && emParams.MatsDhvRecord.UnadjustedHrlyValue != null
                        && emParams.CalculatedUnadjustedValue != null)
                    {
                        if (emParams.CurrentSo2MonitorHourlyRecord == null || 
                                (emParams.CurrentSo2MonitorHourlyRecord.ModcCd != "16" || !emParams.MatsDhvRecord.ParameterCd.InList("SO2RE,SO2RH", ","))
                           )
                        {
                            decimal reportedValue = emParams.MatsDhvRecord.UnadjustedHrlyValue.ScientificNotationtoDecimal();
                            decimal calculatedValue = emParams.CalculatedUnadjustedValue.MatsSignificantDigitsFormat(emParams.CurrentOperatingDate.Value, emParams.MatsDhvRecord.UnadjustedHrlyValue).ScientificNotationtoDecimal();

                            if ((reportedValue + calculatedValue) != 0)
                            {
                                decimal percentDifference = Math.Round(100 * Math.Abs(reportedValue - calculatedValue) / ((reportedValue + calculatedValue) / 2), 1, MidpointRounding.AwayFromZero);

                                if (percentDifference > 5m)
                                {
                                    category.CheckCatalogResult = "A";
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