using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Checks.Em.Parameters;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.EmissionsChecks
{
    public class cLMEChecks : cChecks
    {
        public cLMEChecks()
        {
            CheckProcedures = new dCheckProcedure[43];

            CheckProcedures[11] = new dCheckProcedure(LME11);

            CheckProcedures[13] = new dCheckProcedure(LME13);
            CheckProcedures[14] = new dCheckProcedure(LME14);
            CheckProcedures[15] = new dCheckProcedure(LME15);
            CheckProcedures[16] = new dCheckProcedure(LME16);

            CheckProcedures[41] = new dCheckProcedure(LME41);
            CheckProcedures[42] = new dCheckProcedure(LME42);

        }

        #region Checks 11 - 20

        public  string LME11(cCategory Category, ref bool Log) //Check LTFF System
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();

                if (LTFFRecord["mon_sys_id"] == DBNull.Value)
                    Category.CheckCatalogResult = "A";
                else
                {
                    string sysTypeCd;
                    if (LTFFRecord.Row.Table.Columns.Contains("sys_type_cd"))
                        sysTypeCd = cDBConvert.ToString(LTFFRecord["sys_type_cd"]);
                    else
                    {
                        string MonSysID = cDBConvert.ToString(LTFFRecord["mon_sys_id"]);
                        DataView SystemRecords = Category.GetCheckParameter("Monitor_System_Records").ValueAsDataView();
                        string SysFilter = SystemRecords.RowFilter;
                        SystemRecords.RowFilter = AddToDataViewFilter(SysFilter, "mon_sys_id = '" + MonSysID + "'");
                        sysTypeCd = cDBConvert.ToString(SystemRecords[0]["sys_type_cd"]);
                        SystemRecords.RowFilter = SysFilter;
                    }
                    if (sysTypeCd != "LTOL" && sysTypeCd != "LTGS")
                        Category.CheckCatalogResult = "B";
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME11");
            }
            return ReturnVal;
        }

        public  string LME13(cCategory Category, ref bool Log) //Check Long Term Fuel Flow Value
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();
                decimal LTFFVal = cDBConvert.ToDecimal(LTFFRecord["long_term_fuel_flow_value"]);
                if (LTFFVal <= 0)
                    Category.CheckCatalogResult = "A";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME13");
            }
            return ReturnVal;
        }

        public  string LME14(cCategory Category, ref bool Log) //Check Long Term Fuel Flow UOM
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();
                //DataRowView CorrespondingMonSysRec = Category.GetCheckParameter("Current_Monitor_System_Record").ValueAsDataRowView();
                if (LTFFRecord["LTFF_UOM_CD"] == DBNull.Value)
                    Category.CheckCatalogResult = "A";
                else
                {
                    string sysTypeCd;
                    if (LTFFRecord.Row.Table.Columns.Contains("sys_type_cd"))
                        sysTypeCd = cDBConvert.ToString(LTFFRecord["sys_type_cd"]);
                    else
                    {   //screen check
                        string MonSysID = cDBConvert.ToString(LTFFRecord["mon_sys_id"]);
                        DataView SystemRecords = Category.GetCheckParameter("Monitor_System_Records").ValueAsDataView();
                        string SysFilter = SystemRecords.RowFilter;
                        SystemRecords.RowFilter = AddToDataViewFilter(SysFilter, "mon_sys_id = '" + MonSysID + "'");
                        if (SystemRecords.Count == 0)
                            sysTypeCd = "";
                        else
                            sysTypeCd = cDBConvert.ToString(SystemRecords[0]["sys_type_cd"]);
                        SystemRecords.RowFilter = SysFilter;
                    }

                    string LTFFUOMCd = cDBConvert.ToString(LTFFRecord["LTFF_UOM_CD"]);

                    if (sysTypeCd == "LTOL" && (LTFFUOMCd != "LB" && LTFFUOMCd != "GAL"))
                        Category.CheckCatalogResult = "A";
                    else if (sysTypeCd == "LTGS" && LTFFUOMCd != "SCF")
                        Category.CheckCatalogResult = "A";

                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME14");
            }
            return ReturnVal;
        }

        public  string LME15(cCategory Category, ref bool Log) //Check LTFF GCV
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();
                decimal LTFF_GCV = cDBConvert.ToDecimal(LTFFRecord["GROSS_CALORIFIC_VALUE"]);
                if (LTFF_GCV <= 0)
                    Category.CheckCatalogResult = "A";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME15");
            }
            return ReturnVal;
        }

        public  string LME16(cCategory Category, ref bool Log) //Check LTFF GCV UOM
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();

                Category.SetCheckParameter("LME_Gen_LTFF_Heat_Input", null, eParameterDataType.Integer);
                if (LTFFRecord["GCV_UOM_CD"] == DBNull.Value)
                {
                    Category.CheckCatalogResult = "A";
                }
                else
                {
                    string LTFFUOMCd = cDBConvert.ToString(LTFFRecord["LTFF_UOM_CD"]);
                    string GCVUOMCd = cDBConvert.ToString(LTFFRecord["GCV_UOM_CD"]);
                    string FuelCd = cDBConvert.ToString(LTFFRecord["FUEL_CD"]);

                    if (LTFFUOMCd == "LB" && GCVUOMCd != "BTULB")
                        Category.CheckCatalogResult = "A";
                    else if (LTFFUOMCd == "GAL" && GCVUOMCd != "BTUGAL")
                        Category.CheckCatalogResult = "A";
                    else if (LTFFUOMCd == "SCF" && GCVUOMCd != "BTUSCF")
                        Category.CheckCatalogResult = "A";
                    else
                    {
                        decimal gcv = cDBConvert.ToDecimal(LTFFRecord["GROSS_CALORIFIC_VALUE"]);
                        decimal LTFFVal = cDBConvert.ToDecimal(LTFFRecord["long_term_fuel_flow_value"]);

                        if (gcv > 0 && LTFFVal > 0)
                        {
                            decimal LMEGenLTFFHI = (decimal)Math.Round(gcv * LTFFVal / 1000000, 0, MidpointRounding.AwayFromZero);
                            Category.SetCheckParameter("LME_Gen_LTFF_Heat_Input", LMEGenLTFFHI, eParameterDataType.Decimal);
                        }

                        // updated specs 2012Q2
                        /*
    Max Expected GCV = Lookup "Upper Value" in "Fuel Type Warning Levels for GCV Cross Check Table" where "Fuel Code - Units Of Measure" column = concatenation of (FuelCode, " - ", LongTermFuelFlowUOMCode )
    Min Expected GCV = Lookup "Lower Value" in "Fuel Type Warning Levels for GCV Cross Check Table" where "Fuel Code - Units Of Measure" column = concatenation of (FuelCode, " - ", LongTermFuelFlowUOMCode )
    Max Allowed GCV  = Lookup "Upper Value" in "Fuel Type Reality Checks for GCV Cross Check Table" where "Fuel Code - Units Of Measure" column = concatenation of (FuelCode, " - ", LongTermFuelFlowUOMCode )
    Min Allowed GCV  = Lookup "Lower Value" in "Fuel Type Reality Checks for GCV Cross Check Table" where "Fuel Code - Units Of Measure" column = concatenation of (FuelCode, " - ", LongTermFuelFlowUOMCode )

    if (Max Allowed GCV is not null AND GrossCalorificValue > Max Allowed GCV) OR (Min Allowed GCV is not null AND GrossCalorificValue < Min Allowed GCV)
        return result B
    else
        if (Min Expected GCV is not null AND GrossCalorificValue < Min Expected GCV) OR (Max Expected GCV is not null AND GrossCalorificValue > Max Expected GCV)
            return result C                     
                         */

                        string sFuelCdUOM = string.Format("{0} - {1}", FuelCd, GCVUOMCd);

                        DataView dvReality = Category.GetCheckParameter("Fuel_Type_Reality_Checks_for_GCV_Cross_Check_Table").AsDataView();
                        DataView dvWarnings = Category.GetCheckParameter("Fuel_Type_Warning_Levels_for_GCV_Cross_Check_Table").AsDataView();

                        string sFilter = string.Format("[Fuel Code - Units of Measure]='{0}'", sFuelCdUOM);
                        dvReality.RowFilter = sFilter;
                        dvWarnings.RowFilter = sFilter;

                        decimal? maxAllowedGCV = null;
                        decimal? minAllowedGCV = null;
                        decimal? maxExpectedGCV = null;
                        decimal? minExpectedGCV = null;

                        if (dvReality.Count >= 1)
                        {
                            maxAllowedGCV = Convert.ToDecimal(dvReality[0]["Upper Value"]);
                            minAllowedGCV = Convert.ToDecimal(dvReality[0]["Lower Value"]);
                        }
                        if (dvWarnings.Count >= 1)
                        {
                            maxExpectedGCV = Convert.ToDecimal(dvWarnings[0]["Upper Value"]);
                            minExpectedGCV = Convert.ToDecimal(dvWarnings[0]["Lower Value"]);
                        }

                        if ((maxAllowedGCV.HasValue && gcv > maxAllowedGCV) || (minAllowedGCV.HasValue && gcv < minAllowedGCV))
                            Category.CheckCatalogResult = "B";
                        else if ((minExpectedGCV.HasValue && gcv < minExpectedGCV) || (maxExpectedGCV.HasValue && gcv > maxExpectedGCV))
                            Category.CheckCatalogResult = "C";
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME16");
            }
            return ReturnVal;
        }

        #endregion

        #region Checks 41 - 50

        public  string LME41(cCategory Category, ref bool Log) //Check LTFF Fuel Flow Period Code
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();
                if (Category.GetCheckParameter("LME_OS").ValueAsBool())
                {
                    int CurrentRptPeriod = Category.GetCheckParameter("Current_Reporting_Period").ValueAsInt();

                    //int quarter = Math.Abs(Math.IEEERemainder((double)CurrentRptPeriod, 4) == 0 ? 4 : (int)Math.IEEERemainder((double)CurrentRptPeriod, 4));
                    //if(quarter == 2)
                    if (cDBConvert.ToInteger(LTFFRecord["quarter"]) == 2)
                    {
                        if (LTFFRecord["fuel_flow_period_cd"] == DBNull.Value)
                            Category.CheckCatalogResult = "A";
                    }
                    else
                    {
                        if (LTFFRecord["fuel_flow_period_cd"] != DBNull.Value)
                            Category.CheckCatalogResult = "B";
                    }
                }
                else
                {
                    if (LTFFRecord["fuel_flow_period_cd"] != DBNull.Value)
                        Category.CheckCatalogResult = "C";
                }

            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME41");
            }
            return ReturnVal;
        }

        public  string LME42(cCategory Category, ref bool Log) //Check LTFF Total Heat Input
        {
            string ReturnVal = "";
            try
            {
                DataRowView LTFFRecord = Category.GetCheckParameter("Current_LTFF_Record").ValueAsDataRowView();
                DataRowView CurrentLocation = Category.GetCheckParameter("Current_Monitor_Plan_Location_Record").ValueAsDataRowView();
                int LocnPosn = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();

                //string CurrentEntity = Category.GetCheckParameter("Current_Entity_Type").ValueAsString();
                decimal LMEGenLTFFHI = Category.GetCheckParameter("LME_Gen_LTFF_Heat_Input").ValueAsDecimal();

                string Location = cDBConvert.ToString(CurrentLocation["location_name"]);

                if (Category.GetCheckParameter("LME_Gen_LTFF_Heat_Input").ParameterValue != null)
                {
                    //int TotalHI = cDBConvert.ToInteger(LTFFRecord["total_heat_input"]);                              

                    decimal[] LcnTotalHI = Category.GetCheckParameter("LME_Total_Heat_Input_Array").ValueAsDecimalArray();
                    if (LcnTotalHI[LocnPosn] >= 0)
                    {
                        Category.AccumCheckAggregate("LME_Total_Heat_Input_Array", LocnPosn, LMEGenLTFFHI);
                        if (Category.GetCheckParameter("LME_OS").ValueAsBool() && cDBConvert.ToString(LTFFRecord["fuel_flow_period_cd"]) == "A")
                            Category.AccumCheckAggregate("LME_April_Heat_Input_Array", LocnPosn, LMEGenLTFFHI);
                    }
                    if (Location.PadRight(Location.Length + 1).Substring(0, 2) == "CP")
                    {
                        if (Category.GetCheckParameter("LME_CP_Total_Heat_Input").ValueAsDecimal() >= 0)
                        {
                            Category.AccumCheckAggregate("LME_CP_Total_Heat_Input", LMEGenLTFFHI);
                            if (Category.GetCheckParameter("LME_OS").ValueAsBool() && cDBConvert.ToString(LTFFRecord["fuel_flow_period_cd"]) == "A")
                                Category.AccumCheckAggregate("LME_CP_April_Heat_Input", LMEGenLTFFHI);
                        }
                    }
                }
                else
                {
                    Category.SetArrayParameter("LME_Total_Heat_Input_Array", LocnPosn, -1.0m);
                    if (Location.PadRight(Location.Length + 1).Substring(0, 2) == "CP")
                        Category.SetCheckParameter("LME_CP_Total_Heat_Input", -1.0m, eParameterDataType.Decimal);
                }

                if (Location.PadRight(Location.Length + 1).Substring(0, 2) == "CP")
                {
                    decimal TotalHI4Lcn = Category.GetCheckParameter("LME_Total_Heat_Input_Array").ValueAsDecimalArray()[LocnPosn];
                    Category.SetArrayParameter("Rpt_Period_Hi_Calculated_Accumulator_Array", LocnPosn, TotalHI4Lcn);
                    decimal AprilHI4Lcn = Category.GetCheckParameter("LME_April_Heat_Input_Array").ValueAsDecimalArray()[LocnPosn];
                    Category.SetArrayParameter("April_HI_Calculated_Accumulator_Array", LocnPosn, AprilHI4Lcn);
                    Category.SetArrayParameter("Expected_Summary_Value_Hi_Array", LocnPosn, true);
                }
                decimal TotalHI = cDBConvert.ToDecimal(LTFFRecord["total_heat_input"]);
                if (TotalHI >= 0)
                {
                    decimal RptPrdHIRptdAccm4Lcn = Category.GetCheckParameter("Rpt_Period_Hi_Calculated_Accumulator_Array").ValueAsDecimalArray()[LocnPosn];
                    if (Location.PadRight(Location.Length + 1).Substring(0, 2) == "CP" && RptPrdHIRptdAccm4Lcn > 0)
                    {
                        if (RptPrdHIRptdAccm4Lcn != Decimal.MinValue)
                            Category.AccumCheckAggregate("Rpt_Period_Hi_Reported_Accumulator_Array", LocnPosn, TotalHI);
                        else
                            Category.SetArrayParameter("Rpt_Period_Hi_Reported_Accumulator_Array", LocnPosn, TotalHI);
                    }

                    if (Category.GetCheckParameter("LME_Gen_LTFF_Heat_Input").ParameterValue != null && LMEGenLTFFHI != TotalHI)
                    {
                        DataView HrlyEmissionsTolerances = Category.GetCheckParameter("Hourly_Emissions_Tolerances_Cross_Check_Table").ValueAsDataView();
                        string xcheckFilter = HrlyEmissionsTolerances.RowFilter;
                        HrlyEmissionsTolerances.RowFilter = AddToDataViewFilter(xcheckFilter, "Parameter = 'HI' and UOM = 'MMBTUHR'");
                        decimal HITolerance = cDBConvert.ToDecimal(HrlyEmissionsTolerances[0]["Tolerance"]);
                        if (Math.Abs(TotalHI - LMEGenLTFFHI) > HITolerance)
                            Category.CheckCatalogResult = "A";
                        HrlyEmissionsTolerances.RowFilter = xcheckFilter;
                    }
                }
                else
                {
                    if (Location.PadRight(Location.Length + 1).Substring(0, 2) == "CP")
                        Category.AccumCheckAggregate("Rpt_Period_Hi_Reported_Accumulator_Array", LocnPosn, -1.0m);
                    Category.CheckCatalogResult = "B";
                }


            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "LME42");
            }
            return ReturnVal;
        }

        #endregion

    }
}
