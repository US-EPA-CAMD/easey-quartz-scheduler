using System;
using System.Collections.Generic;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Enumerations;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cMATSOperatingHourChecks : cEmissionsChecks
    {

        #region Constructors

        public cMATSOperatingHourChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[17];

            CheckProcedures[1] = new dCheckProcedure(MATSHOD1);
            CheckProcedures[2] = new dCheckProcedure(MATSHOD2);
            CheckProcedures[3] = new dCheckProcedure(MATSHOD3);
            CheckProcedures[4] = new dCheckProcedure(MATSHOD4);
            CheckProcedures[5] = new dCheckProcedure(MATSHOD5);
            CheckProcedures[6] = new dCheckProcedure(MATSHOD6);
            CheckProcedures[7] = new dCheckProcedure(MATSHOD7);
            CheckProcedures[8] = new dCheckProcedure(MATSHOD8);
            CheckProcedures[9] = new dCheckProcedure(MATSHOD9);
            CheckProcedures[10] = new dCheckProcedure(MATSHOD10);
            CheckProcedures[11] = new dCheckProcedure(MATSHOD11);
            CheckProcedures[12] = new dCheckProcedure(MATSHOD12);
            CheckProcedures[13] = new dCheckProcedure(MATSHOD13);
            CheckProcedures[14] = new dCheckProcedure(MATSHOD14);
            CheckProcedures[15] = new dCheckProcedure(MATSHOD15);
            CheckProcedures[16] = new dCheckProcedure(MATSHOD16);
        }

        #endregion

        #region Checks 1-10

        /// <summary>
        ///  MATS Hg: Locate Active Monitor Method
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public  string MATSHOD1(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHgMethodRecord = null;
                emParams.MatsHgParameterCode = null;
                emParams.MatsHgMethodCode = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //Locate MonitorMethodRecordsByHourLocation records where ParameterCode is equal to "HGRE" or "HGRH".
                    DataView MMRecords = cRowFilter.FindRows(emParams.MonitorMethodRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HGRE,HGRH", eFilterConditionStringCompare.InList) });
                    if (MMRecords.Count > 1)
                    {
                        Category.CheckCatalogResult = "A";
                    }
                    else if (MMRecords.Count == 1)
                    {
                        DataRowView MMRow = MMRecords[0];
                        emParams.MatsHgParameterCode = MMRow["PARAMETER_CD"].ToString();
                        emParams.MatsHgMethodCode = MMRow["METHOD_CD"].ToString();
                        Category.SetCheckParameter("Mats_Hg_Method_Record", MMRow);

                        if (emParams.MatsHgMethodCode.InList("ST,CEMST"))
                        {
                            emParams.FlowMhvOptionallyAllowed = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD1");
            }

            return ReturnVal;
        }

        /// <summary>
        /// MATS HCl: Locate Active Monitor Method
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
		public  string MATSHOD2(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHclMethodRecord = null;
                emParams.MatsHclParameterCode = null;
                emParams.MatsHclMethodCode = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //Locate MonitorMethodRecordsByHourLocation records where ParameterCode is equal to "HCLRE" or "HCLRH".
                    DataView MMRecords = cRowFilter.FindRows(emParams.MonitorMethodRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HCLRE,HCLRH", eFilterConditionStringCompare.InList) });
                    if (MMRecords.Count > 1)
                    {
                        Category.CheckCatalogResult = "A";
                    }
                    else if (MMRecords.Count == 1)
                    {
                        DataRowView MMRow = MMRecords[0];
                        emParams.MatsHclParameterCode = MMRow["PARAMETER_CD"].ToString();
                        emParams.MatsHclMethodCode = MMRow["METHOD_CD"].ToString();
                        Category.SetCheckParameter("Mats_Hcl_Method_Record", MMRow);
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD2");
            }

            return ReturnVal;
        }

        /// <summary>
        /// MATS HF: Locate Active Monitor Method
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
		public  string MATSHOD3(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHfMethodRecord = null;
                emParams.MatsHfParameterCode = null;
                emParams.MatsHfMethodCode = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //Locate MonitorMethodRecordsByHourLocation records where ParameterCode is equal to "HFRE" or "HFRH".
                    DataView MMRecords = cRowFilter.FindRows(emParams.MonitorMethodRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HFRE,HFRH", eFilterConditionStringCompare.InList) });
                    if (MMRecords.Count > 1)
                    {
                        Category.CheckCatalogResult = "A";
                    }
                    else if (MMRecords.Count == 1)
                    {
                        DataRowView MMRow = MMRecords[0];
                        emParams.MatsHfParameterCode = MMRow["PARAMETER_CD"].ToString();
                        emParams.MatsHfMethodCode = MMRow["METHOD_CD"].ToString();
                        Category.SetCheckParameter("Mats_Hf_Method_Record", MMRow);
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD3");
            }

            return ReturnVal;
        }

        /// <summary>
        /// MATS SO2: Locate Active Monitor Method
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
		public  string MATSHOD4(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsSo2MethodRecord = null;
                emParams.MatsSo2ParameterCode = null;
                emParams.MatsSo2MethodCode = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //Locate MonitorMethodRecordsByHourLocation records where ParameterCode is equal to "SO2RE" or "SO2RH".
                    DataView MMRecords = cRowFilter.FindRows(emParams.MonitorMethodRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "SO2RE,SO2RH", eFilterConditionStringCompare.InList) });
                    if (MMRecords.Count > 1)
                    {
                        Category.CheckCatalogResult = "A";
                    }
                    else if (MMRecords.Count == 1)
                    {
                        DataRowView MMRow = MMRecords[0];
                        emParams.MatsSo2ParameterCode = MMRow["PARAMETER_CD"].ToString();
                        emParams.MatsSo2MethodCode = MMRow["METHOD_CD"].ToString();
                        Category.SetCheckParameter("Mats_So2_Method_Record", MMRow);
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD4");
            }

            return ReturnVal;
        }

        #region MATSHOD5
        public  string MATSHOD5(cCategory Category, ref bool Log)
        // MATS: Set MATS Expected Flag
        {
            string ReturnVal = "";

            try
            {
                if (emParams.MatsHgParameterCode != null
                    || emParams.MatsHclParameterCode != null
                    || emParams.MatsHfParameterCode != null
                    || emParams.MatsSo2ParameterCode != null)
                {
                    emParams.MatsExpected = true;
                }
                else
                {
                    emParams.MatsExpected = false;
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD5");
            }

            return ReturnVal;
        }

        #endregion

        /// <summary>
        /// 
        /// MATSHOD6
        /// 
        /// MATS Hg: Locate Dervied Hourly Record.
        /// 
        /// This check ensures that MATS Hg DHV rows only exist for a location when the location operated during the hour and
        /// a single Hg method record with either an "HGRE" or "HGRH" parameter code exists for the hour. If the
        /// at least and only one "HGRE" or "HGRH" row exists for the hour, and the DHV parameter is equal to the Hg Method Parameter.
        /// 
        /// A. Initializes:
        /// 
        ///    1. MatsHgcNeeded, MatsHgreDhvChecksNeeded and MatsHgrhDhvChecksNeeded to false.
        ///    2. MatsHgDhvRecord to null.
        ///    3. MatsHgDhvParameterDescription to "MATS Hg Rate". (Used in plugins)
        /// 
        /// B. If the location's operating time for the hour is greater than zero:
        /// 
        ///    1. Ensures the count of HGRE and HGRH DHV is equal to 0 if a MATS Hg Method does not exist(MatsHgParameterCode is null).
        ///    2. If a MATS Hg Method exists(MatsHgParameterCode is NOT null):
        ///       a. Ensures the count of HGRE and HGRH DHV is equal to 1.
        ///       b. If the precceding condition was met, sets MatsHgDhvRecord to the located row.
        ///       c. Ensures the Parameter Code for the DHV row equals the parameter code for the Hg Method(MatsHgParameterCode).
        ///       d. If preceeding conditions are met, sets:
        ///          * MatsHgcNeeded to true.
        ///          * MatsHgreDhvChecksNeeded to true if parameter code is equal to HGRE.
        ///          * MatsHgrhDhvChecksNeeded to true if parameter code is equal to HGRH.
        ///
        /// C. Otherwise (the locations's operating time for the hour is equal to zero)
        ///
        ///    * Ensure the count of HGRE and HGRH DHV is equal to 0
        /// 
        /// </summary>
        /// <param name="Category">Object for the category running the check.</param>
        /// <param name="Log"><obsolete></obsolete></param>
        /// <returns>Returns an error message if an exception occurs, otherwise returns a null string.</returns>
        public  string MATSHOD6(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHgreDhvChecksNeeded = false;
                emParams.MatsHgrhDhvChecksNeeded = false;
                emParams.MatsHgcNeeded = false;
                emParams.MatsHgDhvRecord = null;
                emParams.MatsHgDhvParameterDescription = "MATS Hg Rate";

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView DHVRecords = null;
                    if (emParams.MatsDhvRecordsByHourLocation != null)
                    {
                        DHVRecords = cRowFilter.FindRows(emParams.MatsDhvRecordsByHourLocation.SourceView,
                                                         new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HGRE,HGRH", eFilterConditionStringCompare.InList) });
                        RecordCount = DHVRecords.Count;
                    }

                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHgParameterCode == null)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else /* Method Exists for Hg */
                        {
                            if (RecordCount == 0)
                            {
                                Category.CheckCatalogResult = "E";
                            }
                            else if (RecordCount > 1)
                            {
                                Category.CheckCatalogResult = "B";
                            }
                            else  /* RecordCount is equal to 1 */
                            {
                                Category.SetCheckParameter("Mats_Hg_Dhv_Record", DHVRecords[0]);
                                Category.SetArrayParameter("Apportionment_Hg_Rate_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHgDhvRecord.UnadjustedHrlyValue);
                                Category.SetArrayParameter("MATS_MS1_Hg_MODC_Code_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHgDhvRecord.ModcCd);

                                if (emParams.MatsHgDhvRecord.ParameterCd == emParams.MatsHgParameterCode)
                                {
                                    if (emParams.MatsHgMethodCode == "CALC")
                                    {
                                        if (emParams.MatsHgDhvRecord.EquationCd == null)
                                        {
                                            if (emParams.MatsHgDhvRecord.ModcCd != "38")
                                                Category.CheckCatalogResult = "F";
                                            else
                                                Category.CheckCatalogResult = "H";
                                        }
                                        else
                                        {
                                            if (emParams.MatsHgDhvRecord.EquationCd != "MS-1")
                                                Category.CheckCatalogResult = "F";
                                            else if (emParams.MatsHgDhvRecord.ModcCd != "38")
                                            {
                                                emParams.MatsMs1HgDhvId = emParams.MatsHgDhvRecord.MatsDhvId;
                                                emParams.MatsMs1HgUnadjustedHourlyValue = emParams.MatsHgDhvRecord.UnadjustedHrlyValue;
                                                emParams.MatsParameterPluginHg = emParams.MatsHgDhvRecord.ParameterCd.ToUpper();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (emParams.MatsHgDhvRecord.EquationCd == "MS-1")
                                        {
                                            Category.CheckCatalogResult = "G";
                                        }
                                        else
                                        {
                                            emParams.MatsHgcNeeded = true;

                                            if (emParams.MatsHgDhvRecord.ParameterCd == "HGRE")
                                            {
                                                emParams.MatsHgreDhvChecksNeeded = true;
                                            }
                                            else if (emParams.MatsHgDhvRecord.ParameterCd == "HGRH")
                                            {
                                                emParams.MatsHgrhDhvChecksNeeded = true;
                                            }
                                        }
                                    }
                                }
                                else /* DHV and Method parameter code mismatch  */
                                {
                                    Category.CheckCatalogResult = "C";
                                }

                            }
                        }
                    }

                    else /* Non Operating Hour */
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD6");
            }

            return ReturnVal;
        }

        /// <summary>
        /// 
        /// MATSHOD7
        /// 
        /// MATS HCl: Locate Dervied Hourly Record.
        /// 
        /// This check ensures that MATS HCl DHV rows only exist for a location when the location operated during the hour and
        /// a single Hg method record with either an "HCLRE" or "HCLRH" parameter code exists for the hour. If the
        /// at least and only one "HCLRE" or "HCLRH" row exists for the hour, and the DHV parameter is equal to the HCl Method Parameter.
        /// 
        /// A. Initializes:
        /// 
        ///    1. MatsHclcNeeded, MatsHclreDhvChecksNeeded and MatsHclrhDhvChecksNeeded to false.
        ///    2. MatsHclDhvRecord to null.
        ///    3. MatsHclDhvParameterDescription to "MATS HCl Rate". (Used in plugins)
        /// 
        /// B. If the location's operating time for the hour is greater than zero:
        /// 
        ///    1. Ensures the count of HCLRE and HCLRH DHV is equal to 0 if a MATS HCl Method does not exist(MatsHclParameterCode is null).
        ///    2. If a MATS HCl Method exists(MatsHclParameterCode is NOT null):
        ///       a. Ensures the count of HCLRE and HCLRH DHV is equal to 1.
        ///       b. If the precceding condition was met, sets MatsHclDhvRecord to the located row.
        ///       c. Ensures the Parameter Code for the DHV row equals the parameter code for the HCl Method(MatsHclParameterCode).
        ///       d. If preceeding conditions are met, sets:
        ///          * MatsHclcNeeded to true.
        ///          * MatsHclreDhvChecksNeeded to true if parameter code is equal to HCLRE.
        ///          * MatsHclrhDhvChecksNeeded to true if parameter code is equal to HCLRH.
        ///
        /// C. Otherwise (the locations's operating time for the hour is equal to zero)
        ///
        ///    * Ensure the count of HCLRE and HCLRH DHV is equal to 0
        /// 
        /// </summary>
        /// <param name="Category">Object for the category running the check.</param>
        /// <param name="Log"><obsolete></obsolete></param>
        /// <returns>Returns an error message if an exception occurs, otherwise returns a null string.</returns>
		public  string MATSHOD7(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHclreDhvChecksNeeded = false;
                emParams.MatsHclrhDhvChecksNeeded = false;
                emParams.MatsHclcNeeded = false;
                emParams.MatsHclDhvRecord = null;
                emParams.MatsHclDhvParameterDescription = "MATS HCl Rate";

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView DHVRecords = null;
                    if (emParams.MatsDhvRecordsByHourLocation != null)
                    {
                        DHVRecords = cRowFilter.FindRows(emParams.MatsDhvRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HCLRE,HCLRH", eFilterConditionStringCompare.InList) });
                        RecordCount = DHVRecords.Count;
                    }

                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHclParameterCode == null)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else /* Method Exists for HCl */
                        {
                            if (RecordCount == 0)
                            {
                                Category.CheckCatalogResult = "E";
                            }
                            else if (RecordCount > 1)
                            {
                                Category.CheckCatalogResult = "B";
                            }
                            else /* RecordCount is equal to 1 */
                            {
                                Category.SetCheckParameter("Mats_Hcl_Dhv_Record", DHVRecords[0]);
                                Category.SetArrayParameter("Apportionment_HCL_Rate_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHclDhvRecord.UnadjustedHrlyValue);
                                Category.SetArrayParameter("MATS_MS1_HCL_MODC_Code_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHclDhvRecord.ModcCd);

                                if (emParams.MatsHclDhvRecord.ParameterCd == emParams.MatsHclParameterCode)
                                {
                                    if (emParams.MatsHclMethodCode == "CALC")
                                    {
                                        if (emParams.MatsHclDhvRecord.EquationCd == null)
                                        {
                                            if (emParams.MatsHclDhvRecord.ModcCd != "38")
                                                Category.CheckCatalogResult = "F";
                                            else
                                                Category.CheckCatalogResult = "H";
                                        }
                                        else
                                        {
                                            if (emParams.MatsHclDhvRecord.EquationCd != "MS-1")
                                                Category.CheckCatalogResult = "F";
                                            else if (emParams.MatsHclDhvRecord.ModcCd != "38")
                                            {
                                                emParams.MatsMs1HclDhvId = emParams.MatsHclDhvRecord.MatsDhvId;
                                                emParams.MatsMs1HclUnadjustedHourlyValue = emParams.MatsHclDhvRecord.UnadjustedHrlyValue;
                                                emParams.MatsParameterPluginHcl = emParams.MatsHclDhvRecord.ParameterCd.ToUpper();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (emParams.MatsHclDhvRecord.EquationCd == "MS-1")
                                        {
                                            Category.CheckCatalogResult = "G";
                                        }
                                        else
                                        {
                                            emParams.MatsHclcNeeded = true;

                                            if (emParams.MatsHclDhvRecord.ParameterCd == "HCLRE")
                                            {
                                                emParams.MatsHclreDhvChecksNeeded = true;
                                            }
                                            else if (emParams.MatsHclDhvRecord.ParameterCd == "HCLRH")
                                            {
                                                emParams.MatsHclrhDhvChecksNeeded = true;
                                            }
                                        }
                                    }
                                }
                                else /* DHV and Method parameter code mismatch  */
                                {
                                    Category.CheckCatalogResult = "C";
                                }
                            }
                        }
                    }
                    else /* Non Operating Hour */
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD7");
            }

            return ReturnVal;
        }

        /// <summary>
        /// 
        /// MATSHOD8
        /// 
        /// MATS HF: Locate Dervied Hourly Record.
        /// 
        /// This check ensures that MATS HF DHV rows only exist for a location when the location operated during the hour and
        /// a single HF method record with either an "HFRE" or "HFRH" parameter code exists for the hour. If the
        /// at least and only one "HFRE" or "HFRH" row exists for the hour, and the DHV parameter is equal to the HF Method Parameter.
        /// 
        /// A. Initializes:
        /// 
        ///    1. MatsHfcNeeded, MatsHfreDhvChecksNeeded and MatsHfrhDhvChecksNeeded to false.
        ///    2. MatsHfDhvRecord to null.
        ///    3. MatsHfDhvParameterDescription to "MATS HF Rate". (Used in plugins)
        /// 
        /// B. If the location's operating time for the hour is greater than zero:
        /// 
        ///    1. Ensures the count of HFRE and HFRH DHV is equal to 0 if a MATS HF Method does not exist(MatsHfParameterCode is null).
        ///    2. If a MATS HF Method exists(MatsHfParameterCode is NOT null):
        ///       a. Ensures the count of HFRE and HFRH DHV is equal to 1.
        ///       b. If the precceding condition was met, sets MatsHfDhvRecord to the located row.
        ///       c. Ensures the Parameter Code for the DHV row equals the parameter code for the HF Method(MatsHfParameterCode).
        ///       d. If preceeding conditions are met, sets:
        ///          * MatsHfcNeeded to true.
        ///          * MatsHfreDhvChecksNeeded to true if parameter code is equal to HFRE.
        ///          * MatsHfrhDhvChecksNeeded to true if parameter code is equal to HFRH.
        ///
        /// C. Otherwise (the locations's operating time for the hour is equal to zero)
        ///
        ///    * Ensure the count of HFRE and HFRH DHV is equal to 0
        /// 
        /// </summary>
        /// <param name="Category">Object for the category running the check.</param>
        /// <param name="Log"><obsolete></obsolete></param>
        /// <returns>Returns an error message if an exception occurs, otherwise returns a null string.</returns>
		public  string MATSHOD8(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHfreDhvChecksNeeded = false;
                emParams.MatsHfrhDhvChecksNeeded = false;
                emParams.MatsHfcNeeded = false;
                emParams.MatsHfDhvRecord = null;
                emParams.MatsHfDhvParameterDescription = "MATS HF Rate";

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView DHVRecords = null;
                    if (emParams.MatsDhvRecordsByHourLocation != null)
                    {
                        DHVRecords = cRowFilter.FindRows(emParams.MatsDhvRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HFRE,HFRH", eFilterConditionStringCompare.InList) });
                        RecordCount = DHVRecords.Count;
                    }

                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHfParameterCode == null)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else /* Method Exists for HF */
                        {
                            if (RecordCount == 0)
                            {
                                Category.CheckCatalogResult = "E";
                            }
                            else if (RecordCount > 1)
                            {
                                Category.CheckCatalogResult = "B";
                            }
                            else /* RecordCount is equal to 1 */
                            {
                                Category.SetCheckParameter("Mats_Hf_Dhv_Record", DHVRecords[0]);
                                Category.SetArrayParameter("Apportionment_HF_Rate_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHfDhvRecord.UnadjustedHrlyValue);
                                Category.SetArrayParameter("MATS_MS1_HF_MODC_Code_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsHfDhvRecord.ModcCd);

                                if (emParams.MatsHfDhvRecord.ParameterCd == emParams.MatsHfParameterCode)
                                {
                                    if (emParams.MatsHfMethodCode == "CALC")
                                    {
                                        if (emParams.MatsHfDhvRecord.EquationCd == null)
                                        {
                                            if (emParams.MatsHfDhvRecord.ModcCd != "38")
                                                Category.CheckCatalogResult = "F";
                                            else
                                                Category.CheckCatalogResult = "H";
                                        }
                                        else
                                        {
                                            if (emParams.MatsHfDhvRecord.EquationCd != "MS-1")
                                                Category.CheckCatalogResult = "F";
                                            else if (emParams.MatsHfDhvRecord.ModcCd != "38")
                                            {
                                                emParams.MatsMs1HfDhvId = emParams.MatsHfDhvRecord.MatsDhvId;
                                                emParams.MatsMs1HfUnadjustedHourlyValue = emParams.MatsHfDhvRecord.UnadjustedHrlyValue;
                                                emParams.MatsParameterPluginHf = emParams.MatsHfDhvRecord.ParameterCd.ToUpper();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (emParams.MatsHfDhvRecord.EquationCd == "MS-1")
                                        {
                                            Category.CheckCatalogResult = "G";
                                        }
                                        else
                                        {
                                            emParams.MatsHfcNeeded = true;

                                            if (emParams.MatsHfDhvRecord.ParameterCd == "HFRE")
                                            {
                                                emParams.MatsHfreDhvChecksNeeded = true;
                                            }
                                            else if (emParams.MatsHfDhvRecord.ParameterCd == "HFRH")
                                            {
                                                emParams.MatsHfrhDhvChecksNeeded = true;
                                            }
                                        }
                                    }
                                }
                                else /* DHV and Method parameter code mismatch  */
                                {
                                    Category.CheckCatalogResult = "C";
                                }
                            }
                        }
                    }
                    else /* Non Operating Hour */
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD8");
            }

            return ReturnVal;
        }

        /// <summary>
        /// 
        /// MATSHOD9
        /// 
        /// MATS SO2: Locate Dervied Hourly Record.
        /// 
        /// This check ensures that MATS SO2 DHV rows only exist for a location when the location operated during the hour and
        /// a single SO2 method record with either an "SO2RE" or "SO2RH" parameter code exists for the hour. If the
        /// at least and only one "SO2RE" or "SO2RH" row exists for the hour, and the DHV parameter is equal to the HF Method Parameter.
        /// 
        /// A. Initializes:
        /// 
        ///    1. MatsSo2cNeeded, MatsSo2reDhvChecksNeeded and MatsSo2rhDhvChecksNeeded to false.
        ///    2. MatsSo2DhvRecord to null.
        ///    3. MatsSo2DhvParameterDescription to "MATS SO2 Rate". (Used in plugins)
        /// 
        /// B. If the location's operating time for the hour is greater than zero:
        /// 
        ///    1. Ensures the count of SO2RE and SO2RH DHV is equal to 0 if a MATS SO2 Method does not exist(MatsSo2ParameterCode is null).
        ///    2. If a MATS SO2 Method exists(MatsSo2ParameterCode is NOT null):
        ///       a. Ensures the count of SO2RE and SO2RH DHV is equal to 1.
        ///       b. If the precceding condition was met, sets MatsSo2DhvRecord to the located row.
        ///       c. Ensures the Parameter Code for the DHV row equals the parameter code for the SO2 Method(MatsSo2ParameterCode).
        ///       d. If preceeding conditions are met, sets:
        ///          * MatsSo2cNeeded to true.
        ///          * MatsSo2reDhvChecksNeeded to true if parameter code is equal to SO2RE.
        ///          * MatsSo2rhDhvChecksNeeded to true if parameter code is equal to SO2RH.
        ///
        /// C. Otherwise (the locations's operating time for the hour is equal to zero)
        ///
        ///    * Ensure the count of SO2RE and SO2RH DHV is equal to 0
        /// 
        /// </summary>
        /// <param name="Category">Object for the category running the check.</param>
        /// <param name="Log"><obsolete></obsolete></param>
        /// <returns>Returns an error message if an exception occurs, otherwise returns a null string.</returns>
		public  string MATSHOD9(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsSo2reDhvChecksNeeded = false;
                emParams.MatsSo2rhDhvChecksNeeded = false;
                emParams.MatsSo2cNeeded = false;
                emParams.MatsSo2DhvRecord = null;
                emParams.MatsSo2DhvParameterDescription = "MATS SO2 Rate";

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView DHVRecords = null;
                    if (emParams.MatsDhvRecordsByHourLocation != null)
                    {
                        DHVRecords = cRowFilter.FindRows(emParams.MatsDhvRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "SO2RE,SO2RH", eFilterConditionStringCompare.InList) });
                        RecordCount = DHVRecords.Count;
                    }

                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsSo2ParameterCode == null)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else /* Method Exists for SO2 Surrogate */
                        {
                            if (RecordCount == 0)
                            {
                                Category.CheckCatalogResult = "E";
                            }
                            else if (RecordCount > 1)
                            {
                                Category.CheckCatalogResult = "B";
                            }
                            else /* RecordCount is equal to 1 */
                            {
                                Category.SetCheckParameter("Mats_So2_Dhv_Record", DHVRecords[0]);
                                Category.SetArrayParameter("Apportionment_SO2_Rate_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsSo2DhvRecord.UnadjustedHrlyValue);
                                Category.SetArrayParameter("MATS_MS1_SO2_MODC_Code_Array", (int)emParams.CurrentMonitorPlanLocationPostion, emParams.MatsSo2DhvRecord.ModcCd);

                                if (emParams.MatsSo2DhvRecord.ParameterCd == emParams.MatsSo2ParameterCode)
                                {
                                    if (emParams.MatsSo2MethodCode == "CALC")
                                    {
                                        if (emParams.MatsSo2DhvRecord.EquationCd == null)
                                        {
                                            if (emParams.MatsSo2DhvRecord.ModcCd != "38")
                                                Category.CheckCatalogResult = "F";
                                            else
                                                Category.CheckCatalogResult = "H";
                                        }
                                        else
                                        {
                                            if (emParams.MatsSo2DhvRecord.EquationCd != "MS-1")
                                                Category.CheckCatalogResult = "F";
                                            else if (emParams.MatsSo2DhvRecord.ModcCd != "38")
                                            {
                                                emParams.MatsMs1So2DhvId = emParams.MatsSo2DhvRecord.MatsDhvId;
                                                emParams.MatsMs1So2UnadjustedHourlyValue = emParams.MatsSo2DhvRecord.UnadjustedHrlyValue;
                                                emParams.MatsParameterPluginSo2 = emParams.MatsSo2DhvRecord.ParameterCd.ToUpper();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (emParams.MatsSo2DhvRecord.EquationCd == "MS-1")
                                        {
                                            Category.CheckCatalogResult = "G";
                                        }
                                        else
                                        {
                                            emParams.MatsSo2cNeeded = true;

                                            if (emParams.MatsSo2DhvRecord.ParameterCd == "SO2RE")
                                            {
                                                emParams.MatsSo2reDhvChecksNeeded = true;
                                            }
                                            else if (emParams.MatsSo2DhvRecord.ParameterCd == "SO2RH")
                                            {
                                                emParams.MatsSo2rhDhvChecksNeeded = true;
                                            }
                                        }
                                    }
                                }
                                else /* DHV and Method parameter code mismatch  */
                                {
                                    Category.CheckCatalogResult = "C";
                                }
                            }
                        }
                    }
                    else  /* Non Operating Hour */
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD9");
            }

            return ReturnVal;
        }

        /// <summary>
        /// MATSHOD-10
        /// 
        /// Ensures that HGC MHV rows only existe when expected, exist when expected, and that a miximum of one exists.
        /// 
        /// When a HGC MHV row is expected and only one MatsMhvHgcRecordsByHourLocation row exists:
        /// 
        /// 1) Sets MatsHgcMhvRecord to the one row. (Initialized to null)
        /// 2) Sets MatsHgcMhvChecksNeeded to true. (Initialized to false)
        /// 3) If MatsHgcMhvRecord.SysTypeCd equals "ST", sets FlowMonitorHourlyChecksNeeded to true. (Another check may have already set value to true)
        /// 
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
		public  string MATSHOD10(cCategory Category, ref bool Log)
        // MATS Hg: Locate Monitor Hourly Record
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHgcMhvChecksNeeded = false;
                emParams.MatsHgcMhvRecord = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView MHVRecords = null;
                    if (emParams.MatsMhvHgcRecordsByHourLocation != null)
                    {
                        MHVRecords = cRowFilter.FindRows(emParams.MatsMhvHgcRecordsByHourLocation.SourceView,
                                             new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HGC", eFilterConditionStringCompare.InList) });
                        RecordCount = MHVRecords.Count;
                    }
                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHgcNeeded == false)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else if (RecordCount == 0)
                        {
                            Category.CheckCatalogResult = "B";
                        }
                        else if (RecordCount > 1)
                        {
                            Category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            Category.SetCheckParameter("Mats_Hgc_Mhv_Record", MHVRecords[0]);
                            emParams.MatsHgcMhvChecksNeeded = true;

                            if (emParams.MatsHgcMhvRecord.SysTypeCd == "ST")
                            {
                                emParams.FlowMonitorHourlyChecksNeeded = true;
                            }
                        }
                    }
                    else //OpTime <=0
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD10");
            }

            return ReturnVal;
        }

        #endregion

        #region Checks 11-20

        #region MATSHOD11
        public  string MATSHOD11(cCategory Category, ref bool Log)
        // MATS HCl: Locate Monitor Hourly Record
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHclcMhvChecksNeeded = false;
                emParams.MatsHclcMhvRecord = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView MHVRecords = null;
                    if (emParams.MatsMhvHclcRecordsByHourLocation != null)
                    {
                        MHVRecords = cRowFilter.FindRows(emParams.MatsMhvHclcRecordsByHourLocation.SourceView,
                                                         new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HCLC", eFilterConditionStringCompare.InList) });
                        RecordCount = MHVRecords.Count;
                    }
                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHclcNeeded == false)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else if (RecordCount == 0)
                        {
                            Category.CheckCatalogResult = "B";
                        }
                        else if (RecordCount > 1)
                        {
                            Category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            Category.SetCheckParameter("Mats_Hclc_Mhv_Record", MHVRecords[0]);
                            emParams.MatsHclcMhvChecksNeeded = true;
                        }
                    }
                    else //OpTime <=0
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD11");
            }

            return ReturnVal;
        }

        #endregion

        #region MATSHOD12
        public  string MATSHOD12(cCategory Category, ref bool Log)
        // MATS HF: Locate Monitor Hourly Record
        {
            string ReturnVal = "";

            try
            {
                emParams.MatsHfcMhvChecksNeeded = false;
                emParams.MatsHfcMhvRecord = null;

                if (emParams.DerivedHourlyChecksNeeded != null && emParams.DerivedHourlyChecksNeeded == true)
                {
                    //set RecordCount
                    int RecordCount = 0;
                    DataView MHVRecords = null;
                    if (emParams.MatsMhvHfcRecordsByHourLocation != null)
                    {
                        MHVRecords = cRowFilter.FindRows(emParams.MatsMhvHfcRecordsByHourLocation.SourceView,
                                                         new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HFC", eFilterConditionStringCompare.InList) });
                        RecordCount = MHVRecords.Count;
                    }
                    if (emParams.CurrentHourlyOpRecord != null && emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if (emParams.MatsHfcNeeded == false)
                        {
                            if (RecordCount > 0)
                            {
                                Category.CheckCatalogResult = "A";
                            }
                        }
                        else if (RecordCount == 0)
                        {
                            Category.CheckCatalogResult = "B";
                        }
                        else if (RecordCount > 1)
                        {
                            Category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            Category.SetCheckParameter("Mats_Hfc_Mhv_Record", MHVRecords[0]);
                            emParams.MatsHfcMhvChecksNeeded = true;
                        }
                    }
                    else //OpTime <=0
                    {
                        if (RecordCount > 0)
                        {
                            Category.CheckCatalogResult = "D";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD12");
            }

            return ReturnVal;
        }

        #endregion

        /// <summary>
        /// MATS: Check MATS Load
        /// 
        /// Enusres that the MATS Load is reported when the current hour is operating 
        /// and an active HGRE, HCLRE, HFRE or SO2RE method exists.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string MATSHOD13(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                category.SetIntegerArrayParameter("Apportionment_MATS_Load_Array", emParams.CurrentMonitorPlanLocationPostion.Value, null);

                if (emParams.CurrentHourlyOpRecord != null)
                {
                    if (emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        /* Require  */
                        int count = emParams.MonitorMethodRecordsByHour.CountRows(new cFilterCondition[] { new cFilterCondition("PARAMETER_CD", "HGRE,HCLRE,HFRE,SO2RE", eFilterConditionStringCompare.InList) });

                        if (count > 0)
                        {
                            category.SetIntegerArrayParameter("Apportionment_MATS_Load_Array", emParams.CurrentMonitorPlanLocationPostion.Value,
                                                                                               (int?)emParams.CurrentHourlyOpRecord.MatsHourLoad);

                            if (emParams.CurrentHourlyOpRecord.MatsHourLoad == null)
                            {
                                category.CheckCatalogResult = "A";
                            }
                            else
                            {
                                if ((emParams.CurrentHourlyOpRecord.LoadUomCd == "MW") &&
                                    (emParams.CurrentHourlyOpRecord.MatsHourLoad < emParams.CurrentHourlyOpRecord.HrLoad))
                                {
                                    if (emParams.MpStackConfigForHourlyChecks != "MS")
                                    {
                                        category.CheckCatalogResult = "D";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (emParams.CurrentHourlyOpRecord.MatsHourLoad != null)
                            {
                                category.CheckCatalogResult = "B";
                            }
                        }
                    }
                    else
                    {
                        if (emParams.CurrentHourlyOpRecord.MatsHourLoad != null)
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
        /// Update Sorbent Trap Operating Date List
        /// </summary>
        /// <param name="Category">The category object for the category in which the check is running.</param>
        /// <param name="Log">Obsolete.</param>
        /// <returns>Returns an exception message if the check fails to run normally.</returns>
        public  string MATSHOD14(cCategory Category, ref bool Log)
        {
            string ReturnVal = "";

            try
            {
                if (emParams.CurrentHourlyOpRecord != null)
                {
                    foreach (SorbentTrapEvalInformation sorbentTrap in emParams.MatsSorbentTrapListByLocationArray[emParams.CurrentMonitorPlanLocationPostion.Value])
                    {
                        if ((emParams.CurrentHourlyOpRecord.OpTime > 0) &&
                            (emParams.CurrentOperatingDate.Value >= sorbentTrap.SorbentTrapBeginDateHour.Value.Date) &&
                            (emParams.CurrentOperatingDate.Value <= sorbentTrap.SorbentTrapEndDateHour.Value.Date) &&
                            (!sorbentTrap.OperatingDateList.Contains(emParams.CurrentOperatingDate.Value)))
                        {
                            sorbentTrap.OperatingDateList.Add(emParams.CurrentOperatingDate.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "MATSHOD14");
            }

            return ReturnVal;
        }

        public  string MATSHOD15(cCategory category, ref bool log)
        // For operating hours, this check inserts the current date into the SorbentTrapDictionary.OperatingDateList for the currrent monitoring location.
        {
            string returnVal = "";

            try
            {
                emParams.MatsHclDhvParameterDescription = "HCLRE or HCLRH";
                emParams.MatsHclMhvParameterDescription = "HCLC";
                emParams.MatsHfDhvParameterDescription = "HFRE or HFRH";
                emParams.MatsHfMhvParameterDescription = "HFC";
                emParams.MatsHgDhvParameterDescription = "HGRE or HGRH";
                emParams.MatsHgMhvParameterDescription = "HGC";
                emParams.MatsSo2DhvParameterDescription = "SO2RE or SO2RH";
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Verifies that a GFM exists for each passed or uncertain sampling trains active for a particular 
        /// operating hour when the HgC MODC is NOT 41 or 42.  Allows GFM records for any operating hour as long as an 
        /// associated sampling trains exists, but does not allow GFM for non operating hours.
        /// 
        /// Note that DerivedHourlyChecksNeeded indicates whether any problems exists with the HRLY_OP_DATA record(s) 
        /// for the hour.
        /// </summary>
        /// <param name="Category">The category object for the category in which the check is running.</param>
        /// <param name="Log">Obsolete.</param>
        /// <returns>Returns an exception message if the check fails to run normally.</returns>
        public  string MATSHOD16(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MatsMissingGfmList = "";
                emParams.MatsMultipleGfmList = "";

                if (emParams.DerivedHourlyChecksNeeded == true)
                {
                    if (emParams.CurrentHourlyOpRecord.OpTime > 0)
                    {
                        if ((emParams.MatsHgcMhvRecord == null) || (emParams.MatsHgcMhvRecord.ModcCd.NotInList("41,42")))
                        {
                            CheckDataView<MatsSamplingTrainRecord> locatedMatsSamplingTrainRecords
                                = emParams.MatsSamplingTrainRecords.FindRows(new cFilterCondition("MON_LOC_ID", emParams.CurrentHourlyOpRecord.MonLocId),
                                                                                 new cFilterCondition("BEGIN_DATEHOUR", eFilterConditionRelativeCompare.LessThanOrEqual, emParams.CurrentDateHour.Value, eNullDateDefault.Min),
                                                                                 new cFilterCondition("END_DATEHOUR", eFilterConditionRelativeCompare.GreaterThanOrEqual, emParams.CurrentDateHour.Value, eNullDateDefault.Max));

                            foreach (MatsSamplingTrainRecord matsSamplingTrainRecord in locatedMatsSamplingTrainRecords)
                            {
                                int gfmCount = emParams.MatsHourlyGfmRecordsForHourAndLocation.CountRows(new cFilterCondition[] { new cFilterCondition("COMPONENT_ID", matsSamplingTrainRecord.ComponentId) });

                                if (gfmCount == 0)
                                {
                                    if (matsSamplingTrainRecord.TrainQaStatusCd.InList("PASSED,UNCERTAIN") && ((emParams.MatsHgcMhvRecord == null) || (emParams.MatsHgcMhvRecord.ModcCd != "34")))
                                    {
                                        if ((emParams.MatsHgcMhvRecord != null) && (emParams.MatsHgcMhvRecord.ModcCd == "32"))
                                        {
                                            int trainCount = locatedMatsSamplingTrainRecords.CountRows(new cFilterCondition[] {
                                                                                                                                new cFilterCondition("COMPONENT_ID", matsSamplingTrainRecord.ComponentId),
                                                                                                                                new cFilterCondition("TRAP_MODC_CD", "32"),
                                                                                                                                new cFilterCondition("TRAIN_QA_STATUS_CD", "PASSED,UNCERTAIN", eFilterConditionStringCompare.InList, true)
                                                                                                                              });

                                            if (trainCount == 0)
                                            {
                                                emParams.MatsMissingGfmList = emParams.MatsMissingGfmList.ListAdd(matsSamplingTrainRecord.Description);
                                            }
                                        }
                                        else
                                        {
                                            emParams.MatsMissingGfmList = emParams.MatsMissingGfmList.ListAdd(matsSamplingTrainRecord.Description);
                                        }
                                    }
                                }
                                else if (gfmCount > 1)
                                {
                                    emParams.MatsMultipleGfmList = emParams.MatsMultipleGfmList.ListAdd(matsSamplingTrainRecord.Description);
                                }
                            }

                            if ((emParams.MatsMissingGfmList != "") && (emParams.MatsMultipleGfmList != ""))
                            {
                                category.CheckCatalogResult = "A";
                            }
                            else if (emParams.MatsMissingGfmList != "")
                            {
                                category.CheckCatalogResult = "B";
                            }
                            else if (emParams.MatsMultipleGfmList != "")
                            {
                                category.CheckCatalogResult = "C";
                            }

                            emParams.MatsMissingGfmList = emParams.MatsMissingGfmList.FormatList();
                            emParams.MatsMultipleGfmList = emParams.MatsMultipleGfmList.FormatList();
                        }
                    }
                    else
                    {
                        if (emParams.MatsHourlyGfmRecordsForHourAndLocation.CountRows(new cFilterCondition[] { new cFilterCondition("MON_LOC_ID", emParams.CurrentHourlyOpRecord.MonLocId) }) > 0)
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

        #endregion

    }
}