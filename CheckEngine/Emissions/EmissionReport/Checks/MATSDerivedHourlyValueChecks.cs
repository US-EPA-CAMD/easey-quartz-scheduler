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
    public class cMATSDerivedHourlyValueChecks : cEmissionsChecks
    {

        #region Constructors

        public cMATSDerivedHourlyValueChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[19];

            CheckProcedures[1] = new dCheckProcedure(MATSDHV1);
            CheckProcedures[2] = new dCheckProcedure(MATSDHV2);
            CheckProcedures[3] = new dCheckProcedure(MATSDHV3);
            CheckProcedures[4] = new dCheckProcedure(MATSDHV4);
            CheckProcedures[5] = new dCheckProcedure(MATSDHV5);
            CheckProcedures[6] = new dCheckProcedure(MATSDHV6);
            CheckProcedures[7] = new dCheckProcedure(MATSDHV7);
            CheckProcedures[8] = new dCheckProcedure(MATSDHV8);
            CheckProcedures[9] = new dCheckProcedure(MATSDHV9);
            CheckProcedures[10] = new dCheckProcedure(MATSDHV10);
            CheckProcedures[11] = new dCheckProcedure(MATSDHV11);
            CheckProcedures[12] = new dCheckProcedure(MATSDHV12);
            CheckProcedures[13] = new dCheckProcedure(MATSDHV13);
            CheckProcedures[14] = new dCheckProcedure(MATSDHV14);
            CheckProcedures[15] = new dCheckProcedure(MATSDHV15);
            CheckProcedures[16] = new dCheckProcedure(MATSDHV16);
            CheckProcedures[17] = new dCheckProcedure(MATSDHV17);
            CheckProcedures[18] = new dCheckProcedure(MATSDHV18);

        }


        #endregion

        #region Checks 1-10

        #region MATSDHV1
        public  string MATSDHV1(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for Hg      
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HGRE";
                emParams.MatsDhvRecord = emParams.MatsHgDhvRecord;
                emParams.MatsEquationCodeWithH2o = "A-3";
                emParams.MatsEquationCodeWithoutH2o = "A-2";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV1");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV2
        public  string MATSDHV2(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for Hg
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HGRH";
                emParams.MatsDhvRecord = emParams.MatsHgDhvRecord;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV2");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV3
        public  string MATSDHV3(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for HCL
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HCLRE";
                emParams.MatsDhvRecord = emParams.MatsHclDhvRecord;
                emParams.MatsEquationCodeWithH2o = "HC-3";
                emParams.MatsEquationCodeWithoutH2o = "HC-2";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV3");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV4
        public  string MATSDHV4(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for HCL
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HCLRH";
                emParams.MatsDhvRecord = emParams.MatsHclDhvRecord;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV4");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV5
        public  string MATSDHV5(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for HF
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HFRE";
                emParams.MatsDhvRecord = emParams.MatsHfDhvRecord;
                emParams.MatsEquationCodeWithH2o = "HF-3";
                emParams.MatsEquationCodeWithoutH2o = "HF-2";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV5");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV6
        public  string MATSDHV6(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for HF
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "HFRH";
                emParams.MatsDhvRecord = emParams.MatsHfDhvRecord;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV6");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV7
        public  string MATSDHV7(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for SO2
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "SO2RE";
                emParams.MatsDhvRecord = emParams.MatsSo2DhvRecord;
                emParams.MatsEquationCodeWithH2o = "S-3";
                emParams.MatsEquationCodeWithoutH2o = "S-2";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV7");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV8
        public  string MATSDHV8(cCategory Category, ref bool Log)
        // This check sets generic parameters and output parameters for subsequent derived hourly checks for SO2
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentDhvParameter = "SO2RH";
                emParams.MatsDhvRecord = emParams.MatsSo2DhvRecord;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV8");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV9
        public  string MATSDHV9(cCategory Category, ref bool Log)
        // Basic check to ensure that Mats MODC reported in the DHV record is valid.
        {
            string ReturnVal = "";

            try
            {
                emParams.DerivedHourlyModcStatus = false;

                if (emParams.MatsDhvRecord.ModcCd.InList("36,38"))
                {
                    emParams.DerivedHourlyModcStatus = true;
                }

                else if (emParams.MatsDhvRecord.ModcCd == "37")
                {
                    if (emParams.MatsDhvRecord.ParameterCd.InList("HGRH,HCLRH,HFRH,SO2RH") && (emParams.CurrentHourlyOpRecord.MatsStartupShutdownFlg != null))
                    {
                        emParams.DerivedHourlyModcStatus = true;
                    }
                    else
                        Category.CheckCatalogResult = "B";
                }

                else if (emParams.MatsDhvRecord.ModcCd == "39")
                {
                    if (emParams.MatsDhvRecord.ParameterCd.InList("HGRE,HCLRE,HFRE,SO2RE") && (emParams.CurrentHourlyOpRecord.MatsStartupShutdownFlg != null))
                    {
                        emParams.DerivedHourlyModcStatus = true;
                    }
                    else
                        Category.CheckCatalogResult = "C";
                }

                else
                    Category.CheckCatalogResult = "A";

            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV9");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV10
        public  string MATSDHV10(cCategory Category, ref bool Log)
        // Checks the Formula ID in the MATS Derived Hourly Value record and ensures that it can be used for the calculation
        {
            string ReturnVal = "";

            try
            {
                emParams.DerivedHourlyFormulaStatus = false;

                if (emParams.DerivedHourlyModcStatus == true)
                {
                    if (emParams.MatsDhvRecord.MonFormId == null)
                    {
                        if (emParams.MatsDhvRecord.ModcCd == "38")
                        {
                            Category.CheckCatalogResult = "G";
                        }
                        else
                        {
                            Category.CheckCatalogResult = "A";
                        }
                    }

                    else  //EquationCode not null
                    {
                        if (emParams.MatsDhvRecord.FormulaActiveInd != 1)
                        {
                            Category.CheckCatalogResult = "B";
                        }
                        else if (emParams.MatsDhvRecord.FormulaParameterCd != emParams.CurrentDhvParameter)
                        {
                            Category.CheckCatalogResult = "C";
                        }
                        else if (emParams.CurrentDhvParameter.InList("HGRE,HCLRE,HFRE,SO2RE") && emParams.MatsDhvRecord.ModcCd == "37")
                        {
                            Category.CheckCatalogResult = "D";
                        }
                        else if (emParams.CurrentDhvParameter.InList("HGRH,HCLRH,HFRH,SO2RH") && emParams.MatsDhvRecord.ModcCd == "39")
                        {
                            Category.CheckCatalogResult = "E";
                        }
                        else
                        {
                            emParams.DerivedHourlyFormulaStatus = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV10");
            }

            return ReturnVal;
        }
        #endregion

        #endregion

        #region Checks 11-20

        #region MATSDHV11
        public  string MATSDHV11(cCategory Category, ref bool Log)
        // Gets Equation Code from Mats Active Monitor Formula Record and verifies that it is an appropriate equation for calculation of HCLRE,HFRE,HGRE,SO2RE
        {
            string ReturnVal = "";

            try
            {
                emParams.DerivedHourlyEquationStatus = false;

                if (emParams.DerivedHourlyFormulaStatus == true)
                {
                    if (emParams.MatsDhvRecord.EquationCd != null)
                    {

                        if (emParams.MatsDhvRecord.EquationCd == emParams.MatsEquationCodeWithoutH2o)
                        {
                            emParams.DerivedHourlyEquationStatus = true;
                            emParams.FlowMonitorHourlyChecksNeeded = true;
                        }
                        else if (emParams.MatsDhvRecord.EquationCd == emParams.MatsEquationCodeWithH2o)
                        {
                            emParams.DerivedHourlyEquationStatus = true;
                            emParams.FlowMonitorHourlyChecksNeeded = true;
                            emParams.MoistureNeeded = true;
                            emParams.H2oMissingDataApproach = emParams.H2oMissingDataApproach.ListAdd("MIN");
                        }
                        else
                        {
                            Category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        emParams.DerivedHourlyEquationStatus = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV11");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV12
        public  string MATSDHV12(cCategory Category, ref bool Log)
        // Gets Mats Equation Code from Active Mats Monitor Formula Record and verifies that it is an appropriate equation for Mats Current parameter.
        {
            string ReturnVal = "";

            try
            {
                emParams.DerivedHourlyEquationStatus = false;

                if (emParams.DerivedHourlyFormulaStatus == true)
                {
                    if (emParams.MatsDhvRecord.EquationCd != null)
                    {

                        if (emParams.MatsDhvRecord.EquationCd.InList("19-1,19-2,19-3,19-3D,19-4,19-5,19-5D,19-6,19-7,19-8,19-9"))
                        {

                            emParams.DerivedHourlyEquationStatus = true;

                            if (emParams.MatsDhvRecord.EquationCd.InList("19-1,19-4"))
                            {
                                emParams.O2DryNeededForMats = true;
                                emParams.FdFactorNeeded = true;
                            }
                            else if (emParams.MatsDhvRecord.EquationCd.InList("19-3,19-3D,19-5,19-5D"))
                            {
                                emParams.O2WetNeededForMats = true;
                                emParams.FdFactorNeeded = true;
                            }
                            else if (emParams.MatsDhvRecord.EquationCd.InList("19-2"))
                            {
                                emParams.O2WetNeededForMats = true;
                                emParams.FwFactorNeeded = true;
                            }
                            else if (emParams.MatsDhvRecord.EquationCd.InList("19-6,19-7,19-8,19-9"))
                            {
                                emParams.Co2DiluentNeededForMats = true;
                                emParams.FcFactorNeeded = true;
                            }
                            if (emParams.MatsDhvRecord.EquationCd.InList("19-3,19-3D,19-4,19-5,19-8,19-9"))
                            {
                                emParams.MoistureNeeded = true;
                            }
                        }
                        else
                        {
                            Category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        emParams.DerivedHourlyEquationStatus = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV12");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV13
        public  string MATSDHV13(cCategory Category, ref bool Log)
        // This check assigns parameter specific check parameters used by the associated calculation checks.
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHgDhvParameter = emParams.CurrentDhvParameter;
                emParams.MatsHgDhvValid = emParams.DerivedHourlyEquationStatus;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV13");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV14
        public  string MATSDHV14(cCategory Category, ref bool Log)
        // This check assigns parameter specific check parameters used by the associated calculation checks.
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHclDhvParameter = emParams.CurrentDhvParameter;
                emParams.MatsHclDhvValid = emParams.DerivedHourlyEquationStatus;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV14");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV15
        public  string MATSDHV15(cCategory Category, ref bool Log)
        // This check assigns parameter specific check parameters used by the associated calculation checks.
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHfDhvParameter = emParams.CurrentDhvParameter;
                emParams.MatsHfDhvValid = emParams.DerivedHourlyEquationStatus;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV15");
            }

            return ReturnVal;
        }
        #endregion

        #region MATSDHV16
        public  string MATSDHV16(cCategory Category, ref bool Log)
        // This check assigns parameter specific check parameters used by the associated calculation checks.
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsSo2DhvParameter = emParams.CurrentDhvParameter;
                emParams.MatsSo2DhvValid = emParams.DerivedHourlyEquationStatus;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV16");
            }

            return ReturnVal;
        }
        #endregion


        /// <summary>
        /// This check assigns parameter specific check parameters used by the associated calculation checks.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSDHV17(cCategory Category, ref bool Log)
        
        {
            string ReturnVal = "";

            try
            {
                emParams.DerivedHourlyUnadjustedValueStatus = false;

                if (emParams.DerivedHourlyModcStatus == true)
                {
                    if (emParams.MatsDhvRecord.ModcCd.InList("36,37,39"))
                    {
                        if (string.IsNullOrEmpty(emParams.MatsDhvRecord.UnadjustedHrlyValue))
                        {
                            Category.CheckCatalogResult = "A";
                        }

                        else if (!emParams.MatsDhvRecord.UnadjustedHrlyValue.MatsSignificantDigitsValid(emParams.CurrentOperatingDate.Value))
                        {
                            Category.CheckCatalogResult = "B";
                        }

                        else if (emParams.MatsDhvRecord.UnadjustedHrlyValue.ScientificNotationtoDecimal() < 0)
                        {
                            Category.CheckCatalogResult = "C";
                        }

                        else
                        {
                            emParams.DerivedHourlyUnadjustedValueStatus = true;
                        }
                    }
                    else // MODC 38
                    {
                        if (!string.IsNullOrEmpty(emParams.MatsDhvRecord.UnadjustedHrlyValue))
                        {
                            Category.CheckCatalogResult = "D";
                        }
                        else
                        {
                            emParams.DerivedHourlyUnadjustedValueStatus = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSDHV17");
            }

            return ReturnVal;
        }


        /// <summary>
        /// Sets the CO2 Diluent, O2 Dry and O2 Wet Need For MATS Calculation parameters to the CO2 Diluent, 
        /// O2 Dry and O2 Wet Need For MATS parameters respectively, if the MODC is measured.
        /// 
        /// This will enable other parts of the program to determine whether a supporting pollutant parameter
        /// is needed for calculations, or only needed for other reasons.
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSDHV18(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if ((emParams.DerivedHourlyEquationStatus == true) && (emParams.DerivedHourlyModcStatus == true) && emParams.MatsDhvRecord.ModcCd.InList("36,37,39"))
                {
                    if (emParams.Co2DiluentNeededForMats == true)
                        emParams.Co2DiluentNeededForMatsCalculation = true;

                    if (emParams.O2DryNeededForMats == true)
                        emParams.O2DryNeededForMatsCalculation = true;

                    if (emParams.O2WetNeededForMats == true)
                        emParams.O2WetNeededForMatsCalculation = true;
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