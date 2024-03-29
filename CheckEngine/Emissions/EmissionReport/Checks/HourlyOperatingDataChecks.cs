using System;
using System.Collections.Generic;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Data.EcmpsAux.CrossCheck.Virtual;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

namespace ECMPS.Checks.EmissionsChecks
{
    public class cHourlyOperatingDataChecks : cEmissionsChecks
    {

        #region Constructors

        public cHourlyOperatingDataChecks(cEmissionsReportProcess emissionReportProcess)
          : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[57];

            CheckProcedures[1] = new dCheckProcedure(HOUROP1);
            CheckProcedures[2] = new dCheckProcedure(HOUROP2);
            CheckProcedures[3] = new dCheckProcedure(HOUROP3);
            CheckProcedures[4] = new dCheckProcedure(HOUROP4);
            CheckProcedures[5] = new dCheckProcedure(HOUROP5);
            CheckProcedures[6] = new dCheckProcedure(HOUROP6);
            CheckProcedures[7] = new dCheckProcedure(HOUROP7);
            CheckProcedures[8] = new dCheckProcedure(HOUROP8);
            CheckProcedures[9] = new dCheckProcedure(HOUROP9);
            CheckProcedures[12] = new dCheckProcedure(HOUROP12);
            CheckProcedures[13] = new dCheckProcedure(HOUROP13);
            CheckProcedures[14] = new dCheckProcedure(HOUROP14);
            CheckProcedures[17] = new dCheckProcedure(HOUROP17);
            CheckProcedures[18] = new dCheckProcedure(HOUROP18);
            CheckProcedures[19] = new dCheckProcedure(HOUROP19);
            CheckProcedures[20] = new dCheckProcedure(HOUROP20);
            CheckProcedures[21] = new dCheckProcedure(HOUROP21);
            CheckProcedures[22] = new dCheckProcedure(HOUROP22);
            CheckProcedures[23] = new dCheckProcedure(HOUROP23);
            CheckProcedures[24] = new dCheckProcedure(HOUROP24);
            CheckProcedures[30] = new dCheckProcedure(HOUROP30);
            CheckProcedures[32] = new dCheckProcedure(HOUROP32);
            CheckProcedures[33] = new dCheckProcedure(HOUROP33);
            CheckProcedures[34] = new dCheckProcedure(HOUROP34);
            CheckProcedures[35] = new dCheckProcedure(HOUROP35);
            CheckProcedures[36] = new dCheckProcedure(HOUROP36);
            CheckProcedures[37] = new dCheckProcedure(HOUROP37);
            CheckProcedures[38] = new dCheckProcedure(HOUROP38);
            CheckProcedures[39] = new dCheckProcedure(HOUROP39);
            CheckProcedures[40] = new dCheckProcedure(HOUROP40);
            CheckProcedures[41] = new dCheckProcedure(HOUROP41);
            CheckProcedures[42] = new dCheckProcedure(HOUROP42);
            CheckProcedures[43] = new dCheckProcedure(HOUROP43);
            CheckProcedures[44] = new dCheckProcedure(HOUROP44);
            CheckProcedures[45] = new dCheckProcedure(HOUROP45);
            CheckProcedures[46] = new dCheckProcedure(HOUROP46);
            CheckProcedures[47] = new dCheckProcedure(HOUROP47);
            CheckProcedures[48] = new dCheckProcedure(HOUROP48);
            CheckProcedures[49] = new dCheckProcedure(HOUROP49);
            CheckProcedures[50] = new dCheckProcedure(HOUROP50);
            CheckProcedures[51] = new dCheckProcedure(HOUROP51);
            CheckProcedures[52] = new dCheckProcedure(HOUROP52);
            CheckProcedures[53] = new dCheckProcedure(HOUROP53);
            CheckProcedures[54] = new dCheckProcedure(HOUROP54);
            CheckProcedures[55] = new dCheckProcedure(HOUROP55);
            CheckProcedures[56] = new dCheckProcedure(HOUROP56);
        }

        #endregion


        #region Public  Methods: Checks (1 - 10)

        public  string HOUROP1(cCategory Category, ref bool Log)
        // Validate Single Operating Data record for hour        
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("Current_Hourly_Op_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Unit_Hourly_Operational_Status", false, eParameterDataType.Boolean);
                emParams.CurrentOperatingTime = null;
                Category.SetCheckParameter("Hourly_Extraneous_Fields", null, eParameterDataType.String);

                DataView HourlyOperatingDataView = (DataView)Category.GetCheckParameter("Hourly_Operating_Data_Records_By_Hour_Location").ParameterValue;
                bool AnnRptRequ = Convert.ToBoolean(Category.GetCheckParameter("Annual_Reporting_Requirement").ParameterValue);
                DateTime CurrentOpDate = Category.GetCheckParameter("Current_Operating_Date").ValueAsDateTime(DateTypes.START);
                string EntityType = Convert.ToString(Category.GetCheckParameter("Current_Entity_Type").ParameterValue);
                string CurrMonth = Convert.ToString(Category.GetCheckParameter("Current_Month").ParameterValue);

                if (HourlyOperatingDataView.Count == 0)
                {
                    Category.SetCheckParameter("Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);

                    if (CurrMonth != "April" || AnnRptRequ)
                        if (EntityType == "Unit" || Category.GetCheckParameter("LME_HI_Method").ParameterValue == null)
                            if (!Category.GetCheckParameter("Reporting_Period_Operating").ValueAsBool()
                                && Category.GetCheckParameter("Legacy_Data_Evaluation").ValueAsBool())
                                Category.CheckCatalogResult = "E";
                            else
                            {
                                DataView MonitorMethodRecords = Category.GetCheckParameter("Monitor_Method_Records_By_Hour_Location").ValueAsDataView();

                                if (MonitorMethodRecords.Count > 0)
                                    Category.CheckCatalogResult = "A";
                            }
                }

                else if (HourlyOperatingDataView.Count > 1)
                {
                    if (CurrMonth != "April" || AnnRptRequ)
                    {
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_HI_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_HI_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_NOx_Rate_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_NOx_Rate_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_SO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_SO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_Op_Time_Accumulator_Array", CurrentLocationPos, -1m);
                        Category.SetArrayParameter("Rpt_Period_Op_Hours_Accumulator_Array", CurrentLocationPos, -1);
                        Category.SetArrayParameter("Daily_Op_Time_Accumulator_Array", CurrentLocationPos, -1m);
                    }
                    Category.SetCheckParameter("Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.CheckCatalogResult = "B";
                }

                else
                  if (EntityType != "Unit" && Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                {
                    Category.SetCheckParameter("Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.CheckCatalogResult = "D";
                }
                else
                {
                    decimal OpTime = cDBConvert.ToDecimal(HourlyOperatingDataView[0]["OP_TIME"]);

                    Category.SetCheckParameter("Current_Hourly_Op_Record", HourlyOperatingDataView[0], eParameterDataType.DataRowView);
                    Category.SetCheckParameter("Current_Operating_Time", OpTime, eParameterDataType.Decimal);
                    int thisLocation = cDBConvert.ToInteger(Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ParameterValue);
                    if (Category.GetCheckParameter("First_Day_of_Operation").AsBeginDateTime() == DateTime.MinValue)
                    {
                        Category.SetCheckParameter("First_Day_of_Operation", cDBConvert.ToDate(HourlyOperatingDataView[0]["BEGIN_DATE"], DateTypes.START), eParameterDataType.Date);
                        Category.SetCheckParameter("First_Hour_of_Operation", cDBConvert.ToInteger(HourlyOperatingDataView[0]["BEGIN_HOUR"]), eParameterDataType.Integer);
                    }

                    if (OpTime < 0 || 1 < OpTime)
                    {
                        Category.SetCheckParameter("Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                        if (CurrMonth != "April" || AnnRptRequ)
                        {
                            Category.SetArrayParameter("Rpt_Period_Op_Time_Accumulator_Array", thisLocation, -1m);
                            Category.SetArrayParameter("Rpt_Period_Op_Hours_Accumulator_Array", thisLocation, -1);
                            Category.SetArrayParameter("Daily_Op_Time_Accumulator_Array", thisLocation, -1m);
                        }
                        if (EntityType == "Unit")
                            Category.SetCheckParameter("Unit_Optime_Accumulator", -1, eParameterDataType.Decimal);
                        else
                            Category.SetCheckParameter("Stack_Optime_Accumulator", -1, eParameterDataType.Decimal);
                        Category.CheckCatalogResult = "C";
                    }
                    else
                    {
                        Category.SetCheckParameter("Derived_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                        if (OpTime > 0)
                        {
                            Category.SetCheckParameter("Unit_Hourly_Operational_Status", true, eParameterDataType.Boolean);

                            /* Adds the current date to the opearting date list for the location, if the date is not in the list. */
                            if (!emParams.OperatingDateArray[thisLocation].Contains(emParams.HourlyOperatingDataRecordsByHourLocation[0].BeginDate.AsStartDate()))
                            {
                                emParams.OperatingDateArray[thisLocation].Add(emParams.HourlyOperatingDataRecordsByHourLocation[0].BeginDate.AsStartDate());
                            }

                            if (CurrMonth != "April" || AnnRptRequ)
                            {
                                int[] OpHrsAccumArray = Category.GetCheckParameter("Rpt_Period_Op_Hours_Accumulator_Array").ValueAsIntArray();
                                if (OpHrsAccumArray[thisLocation] != int.MinValue)
                                {
                                    if (OpHrsAccumArray[thisLocation] >= 0)
                                        Category.AccumCheckAggregate("Rpt_Period_Op_Hours_Accumulator_Array", thisLocation, 1);
                                }
                                else
                                    Category.SetArrayParameter("Rpt_Period_Op_Hours_Accumulator_Array", thisLocation, 1);

                                decimal[] RptPerOpAccumArray = Category.GetCheckParameter("Rpt_Period_Op_Time_Accumulator_Array").ValueAsDecimalArray();
                                if (RptPerOpAccumArray[thisLocation] != decimal.MinValue)
                                {
                                    if (RptPerOpAccumArray[thisLocation] >= 0)
                                        Category.AccumCheckAggregate("Rpt_Period_Op_Time_Accumulator_Array", thisLocation, OpTime);
                                }
                                else
                                    Category.SetArrayParameter("Rpt_Period_Op_Time_Accumulator_Array", thisLocation, OpTime);
                                if (CurrMonth == "April")
                                {
                                    int[] AprilOpHrsAccumArray = Category.GetCheckParameter("April_Op_Hours_Accumulator_Array").ValueAsIntArray();
                                    if (AprilOpHrsAccumArray[thisLocation] != int.MinValue)
                                        Category.AccumCheckAggregate("April_Op_Hours_Accumulator_Array", thisLocation, 1);
                                    else
                                        Category.SetArrayParameter("April_Op_Hours_Accumulator_Array", thisLocation, 1);

                                    decimal[] AprilOpAccumArray = Category.GetCheckParameter("April_Op_Time_Accumulator_Array").ValueAsDecimalArray();
                                    if (AprilOpAccumArray[thisLocation] != decimal.MinValue)
                                        Category.AccumCheckAggregate("April_Op_Time_Accumulator_Array", thisLocation, OpTime);
                                    else
                                        Category.SetArrayParameter("April_Op_Time_Accumulator_Array", thisLocation, OpTime);
                                }
                            }

                            decimal[] DailyOpAccumArray = Category.GetCheckParameter("Daily_Op_Time_Accumulator_Array").ValueAsDecimalArray();
                            if (DailyOpAccumArray[thisLocation] != decimal.MinValue)
                            {
                                if (DailyOpAccumArray[thisLocation] >= 0)
                                    Category.AccumCheckAggregate("Daily_Op_Time_Accumulator_Array", thisLocation, OpTime);
                            }
                            else
                                Category.SetArrayParameter("Daily_Op_Time_Accumulator_Array", thisLocation, OpTime);

                            string[] LastDayArray = Category.GetCheckParameter("Last_Day_of_Operation_Array").ValueAsStringArray();
                            if (LastDayArray[thisLocation] == null || LastDayArray[thisLocation] == "" || LastDayArray[thisLocation] != CurrentOpDate.ToShortDateString())
                            {
                                Category.SetArrayParameter("Last_Day_of_Operation_Array", thisLocation, CurrentOpDate.ToShortDateString());

                                int[] RptPerOpDaysAccumArray = Category.GetCheckParameter("Rpt_Period_Op_Days_Accumulator_Array").ValueAsIntArray();
                                if (RptPerOpDaysAccumArray[thisLocation] != int.MinValue)
                                {
                                    if (RptPerOpDaysAccumArray[thisLocation] >= 0)
                                        Category.AccumCheckAggregate("Rpt_Period_Op_Days_Accumulator_Array", thisLocation, 1);
                                }
                                else
                                    Category.SetArrayParameter("Rpt_Period_Op_Days_Accumulator_Array", thisLocation, 1);

                                int[] AprilOpDaysAccumArray = Category.GetCheckParameter("April_Op_Days_Accumulator_Array").ValueAsIntArray();
                                if (CurrMonth == "April")
                                    if (AprilOpDaysAccumArray[thisLocation] != int.MinValue)
                                        Category.AccumCheckAggregate("April_Op_Days_Accumulator_Array", thisLocation, 1);
                                    else
                                        Category.SetArrayParameter("April_Op_Days_Accumulator_Array", thisLocation, 1);
                            }
                            if (EntityType == "Unit")
                            {
                                decimal UnitAccum = Convert.ToDecimal(Category.GetCheckParameter("Unit_Optime_Accumulator").ParameterValue);
                                if (UnitAccum >= 0)
                                    Category.SetCheckParameter("Unit_Optime_Accumulator", UnitAccum + OpTime, eParameterDataType.Decimal);
                                if (OpTime > Convert.ToDecimal(Category.GetCheckParameter("Max_Unit_Optime").ParameterValue))
                                    Category.SetCheckParameter("Max_Unit_Optime", OpTime, eParameterDataType.Decimal);
                            }
                            else if (EntityType == "CS" || EntityType == "MS")
                            {
                                decimal StackAccum = Convert.ToDecimal(Category.GetCheckParameter("Stack_Optime_Accumulator").ParameterValue);
                                if (StackAccum >= 0)
                                    Category.SetCheckParameter("Stack_Optime_Accumulator", StackAccum + OpTime, eParameterDataType.Decimal);
                                if (OpTime > Convert.ToDecimal(Category.GetCheckParameter("Max_Stack_Optime").ParameterValue))
                                    Category.SetCheckParameter("Max_Stack_Optime", OpTime, eParameterDataType.Decimal);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP1");
            }

            return ReturnVal;
        }

        public  string HOUROP2(cCategory Category, ref bool Log)
        // Count Flow, O2, and Heat Input records
        // Formerly Hourly-79
        {
            string ReturnVal = "";

            try
            {
                DataView FlowMonitorHourlyView = (DataView)Category.GetCheckParameter("Flow_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                DataView O2WetMonitorHourlyView = (DataView)Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                DataView O2DryMonitorHourlyView = (DataView)Category.GetCheckParameter("O2_Dry_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                DataView O2NullMonitorHourlyView = (DataView)Category.GetCheckParameter("O2_Null_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                DataView HiDerivedHourlyView = (DataView)Category.GetCheckParameter("HI_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;

                sFilterPair[] DHVRecordsFilter = new sFilterPair[1];
                DHVRecordsFilter[0].Set("PARAMETER_CD", "HI");
                DataView HiDerivedHourlyValueRecordsFound = FindRows(HiDerivedHourlyView, DHVRecordsFilter);

                Category.SetCheckParameter("Flow_Monitor_Hourly_Count", FlowMonitorHourlyView.Count, eParameterDataType.Integer);
                Category.SetCheckParameter("O2_Wet_Monitor_Hourly_Count", O2WetMonitorHourlyView.Count, eParameterDataType.Integer);
                Category.SetCheckParameter("O2_Dry_Monitor_Hourly_Count", O2DryMonitorHourlyView.Count, eParameterDataType.Integer);
                Category.SetCheckParameter("O2_Null_Monitor_Hourly_Count", O2NullMonitorHourlyView.Count, eParameterDataType.Integer);
                Category.SetCheckParameter("Heat_Input_Derived_Hourly_Count", HiDerivedHourlyValueRecordsFound.Count, eParameterDataType.Integer);

                if (O2NullMonitorHourlyView.Count == 1)
                    Category.SetCheckParameter("Current_O2_Null_Monitor_Hourly_Record", O2NullMonitorHourlyView[0], eParameterDataType.DataRowView);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP2");
            }

            return ReturnVal;
        }

        public  string HOUROP3(cCategory Category, ref bool Log)
        // Unit Information Lookup
        {
            string ReturnVal = "";

            try
            {
                sFilterPair[] RowFilter;

                DataRowView CurrentMonitorPlanLocationRecord = Category.GetCheckParameter("Current_Monitor_Plan_Location_Record").ValueAsDataRowView();
                DateTime currentOperatingDate = emParams.CurrentOperatingDate.Value;

                Category.SetCheckParameter("Special_Fuel_Burned", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("FC_Factor_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("FD_Factor_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("FW_Factor_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Moisture_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Flow_Monitor_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Flow_Needed_For_Part_75", false, eParameterDataType.Boolean);
                emParams.FlowMhvOptionallyAllowed = false;
                Category.SetCheckParameter("H2O_Missing_Data_Approach", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_MHV_Parameter", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_MHV_System_Type", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_MHV_Component_Type", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_MHV_Max_Min_Value", null, eParameterDataType.String);

                Category.SetCheckParameter("Current_DHV_Parameter", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_DHV_Record_Valid", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_DHV_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_DHV_Method", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_DHV_System_Type", null, eParameterDataType.String);
                Category.SetCheckParameter("Current_DHV_HBHA_Value", null, eParameterDataType.Decimal);
                Category.SetCheckParameter("CO2_Conc_Checks_Needed_For_CO2_Mass_Calc", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("CO2_Conc_Checks_Needed_for_Heat_Input", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("CO2_Diluent_Checks_Needed_For_Nox_Rate_Calc", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Dry_Checks_Needed_for_Heat_Input", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Dry_Checks_Needed_For_Nox_Rate_Calc", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Wet_Checks_Needed_for_Heat_Input", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Wet_Checks_Needed_For_Nox_Rate_Calc", false, eParameterDataType.Boolean);

                // 11/7/14 RAB New parameters
                emParams.Co2DiluentNeededForMats = false;
                emParams.Co2DiluentNeededForMatsCalculation = false;
                emParams.O2DryNeededForMats = false;
                emParams.O2DryNeededForMatsCalculation = false;
                emParams.O2WetNeededForMats = false;
                emParams.O2WetNeededForMatsCalculation = false;

                Category.SetCheckParameter("Linearity_Status_Required", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Appendix_E_Status_Required", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("RATA_Status_Required", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_RATA_Status", null, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_Hourly_Record_for_RATA_Status", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("RATA_Status_BAF", null, eParameterDataType.Decimal);
                Category.SetCheckParameter("Daily_Cal_Status_Required", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("CO2_RATA_Required", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("HI_Measure_Code", null, eParameterDataType.String);
                Category.SetCheckParameter("NOXR_Measure_Code", null, eParameterDataType.String);
                Category.SetCheckParameter("F2L_Status_Required", false, eParameterDataType.Boolean);

                // 11/7/14 RAB New parameters
                emParams.Co2cMhvModc = null;
                emParams.H2oDhvModc = null;
                emParams.H2oMhvModc = null;
                emParams.O2DryModc = null;
                emParams.O2WetModc = null;

                emParams.So2HpffExists = false;
                emParams.Co2HpffExists = false;
                emParams.HiHpffExists = false;

                string CurrentEntityType = "";
                {
                    string LocationName = cDBConvert.ToString(CurrentMonitorPlanLocationRecord["Location_Name"]);

                    if (LocationName.PadRight(2).Substring(0, 2) == "CS")
                        CurrentEntityType = "CS";
                    else if (LocationName.PadRight(2).Substring(0, 2) == "CP")
                        CurrentEntityType = "CP";
                    else if (LocationName.PadRight(2).Substring(0, 2) == "MS")
                        CurrentEntityType = "MS";
                    else if (LocationName.PadRight(2).Substring(0, 2) == "MP")
                        CurrentEntityType = "MP";
                    else
                        CurrentEntityType = "Unit";
                }
                Category.SetCheckParameter("Current_Entity_Type", CurrentEntityType, eParameterDataType.String);

                bool CurrentUnitIsPeaking = false;
                {
                    DataView MonitorQualificationRecords = (DataView)(Category.GetCheckParameter("Monitor_Qualification_Records_By_Hour").ParameterValue);

                    if (CurrentEntityType == "Unit")
                    {
                        string MonLocId = cDBConvert.ToString(CurrentMonitorPlanLocationRecord["Mon_Loc_Id"]);

                        RowFilter = new sFilterPair[2];
                        RowFilter[0].Set("Mon_Loc_Id", MonLocId);
                        RowFilter[1].Set("Qual_Type_Cd", "PK,SK", eFilterPairStringCompare.InList);

                        DataView MonitorQualificationView = FindRows(MonitorQualificationRecords, RowFilter);

                        if (MonitorQualificationView.Count > 0)
                            CurrentUnitIsPeaking = true;
                    }
                    else if (CurrentEntityType == "CP")
                    {
                        DataView UnitStackConfigurationRecords = Category.GetCheckParameter("Unit_Stack_Configuration_Records_By_Hour_Location").ValueAsDataView();

                        string LocationIdList = ColumnToDatalist(UnitStackConfigurationRecords, "Mon_Loc_Id");

                        RowFilter = new sFilterPair[2];
                        RowFilter[0].Set("Mon_Loc_Id", LocationIdList, eFilterPairStringCompare.InList);
                        RowFilter[1].Set("Qual_Type_Cd", "PK,SK", eFilterPairStringCompare.InList);

                        DataView MonitorQualificationView = FindRows(MonitorQualificationRecords, RowFilter);

                        if (MonitorQualificationView.Count > 0)
                            CurrentUnitIsPeaking = true;
                    }
                }
                Category.SetCheckParameter("Current_Unit_is_Peaking", CurrentUnitIsPeaking, eParameterDataType.Boolean);

                /* Set Program Related Parameters */
                {
                    bool currentUnitIsArp = false;
                    bool currentUnitIsMats = false;
                    bool currentUnitHasNonMatsSo2Program = false;

                    foreach (VwMpLocationProgramRow programRecord in emParams.LocationProgramRecordsByHourLocation)
                    {
                        DateTime? erbDate = programRecord.EmissionsRecordingBeginDate;
                        DateTime? umcbDate = programRecord.UnitMonitorCertBeginDate;

                        if (((erbDate != null) && (erbDate.Value <= currentOperatingDate)) ||
                            (((erbDate == null) && (umcbDate != null) && (umcbDate.Value <= currentOperatingDate))))
                        {
                            string prgCd = programRecord.PrgCd;
                            string classCd = programRecord.Class;

                            if ((prgCd == "ARP") && (classCd == "P1" || classCd == "P2"))
                            {
                                currentUnitIsArp = true;
                            }

                            if ((prgCd == "MATS") && classCd == "A")
                            {
                                currentUnitIsMats = true;
                            }

                            if (currentUnitIsArp == false)
                            {
                                if ((prgCd != "MATS") && prgCd.InList(emParams.ProgramRequiresSo2SystemCertificationList) && (classCd == "A"))
                                {
                                    currentUnitHasNonMatsSo2Program = true;
                                }
                            }
                        }
                    }

                    emParams.CurrentUnitIsArp = currentUnitIsArp;
                    emParams.So2cIsOnlyForMats = currentUnitIsMats && !currentUnitHasNonMatsSo2Program;
                }

                DateTime EarlyDate;
                {
                    DataRowView MPLRecord = Category.GetCheckParameter("Current_Monitor_Plan_Location_Record").ValueAsDataRowView();
                    EarlyDate = cDBConvert.ToDate(MPLRecord["EARLIEST_REPORT_DATE"], DateTypes.START);
                }
                Category.SetCheckParameter("Earliest_Location_Report_Date", EarlyDate, eParameterDataType.Date);
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP3");
            }

            return ReturnVal;
        }

        public  string HOUROP4(cCategory Category, ref bool Log)
        // Verify SO2 Monitor Method Active During Current Hour
        // Formerly Hourly-11
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("So2_Monitor_Method_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_F23_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_Method_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("So2_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_Bypass_Code", null, eParameterDataType.String);

                    DataView MonitorMethodView = (DataView)Category.GetCheckParameter("SO2_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    if (MonitorMethodView.Count > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (MonitorMethodView.Count == 1)
                    {
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();

                        DataRowView MonitorMethodRow = (DataRowView)MonitorMethodView[0];
                        string MethodCd = cDBConvert.ToString(MonitorMethodRow["Method_Cd"]);
                        Category.SetCheckParameter("SO2_Method_Code", MethodCd, eParameterDataType.String);
                        Category.SetCheckParameter("So2_Monitor_Method_Record", MonitorMethodRow, eParameterDataType.DataRowView);
                        if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null && MethodCd != "LME")
                            Category.CheckCatalogResult = "B";
                        else
                        {
                            if (cDBConvert.ToString(MonitorMethodRow["SUB_DATA_CD"]).StartsWith("FSP75"))
                                Category.SetCheckParameter("So2_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);
                            Category.SetCheckParameter("SO2_Bypass_Code", cDBConvert.ToString(MonitorMethodRow["BYPASS_APPROACH_CD"]), eParameterDataType.String);
                            Category.AccumCheckAggregate("Expected_Summary_Value_So2_Array", CurrentLocationPos, true, true);
                            if (MethodCd == "CEM")
                                Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                            else
                              if (MethodCd == "F23")
                                Category.SetCheckParameter("SO2_F23_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                            else
                                if (MethodCd == "AD")
                                Category.SetCheckParameter("SO2_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        }
                    }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP4");
            }

            return ReturnVal;
        }

        public  string HOUROP5(cCategory category, ref bool log)
        // Determine H2O Method
        // Formerly Hourly-80
        {
            string returnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    category.SetCheckParameter("H2O_Method_Code", null, eParameterDataType.String);
                    category.SetCheckParameter("H2O_Default_Value", null, eParameterDataType.Decimal);
                    category.SetCheckParameter("H2O_Default_Max_Value", null, eParameterDataType.Decimal);
                    category.SetCheckParameter("H2O_Default_Min_Value", null, eParameterDataType.Decimal);
                    category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", null, eParameterDataType.String);
                    category.SetCheckParameter("H2O_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);
                    category.SetCheckParameter("H2O_Reported_Value", null, eParameterDataType.Decimal);

                    DataView h2oDerivedHourlyView = (DataView)category.GetCheckParameter("H2O_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                    category.SetCheckParameter("H2O_Derived_Hourly_Count", h2oDerivedHourlyView.Count, eParameterDataType.Integer);

                    DataView h2oMonitorHourlyView = (DataView)category.GetCheckParameter("H2O_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                    category.SetCheckParameter("H2O_Monitor_Hourly_Count", h2oMonitorHourlyView.Count, eParameterDataType.Integer);

                    DataView h2oMonitorMethodView = (DataView)category.GetCheckParameter("H2O_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    if (h2oMonitorMethodView.Count > 2)
                    {
                        category.CheckCatalogResult = "A";
                    }
                    else if (h2oMonitorMethodView.Count == 2)
                    {
                        if ((h2oDerivedHourlyView.Count + h2oMonitorHourlyView.Count) > 0)
                        {
                            if ((h2oDerivedHourlyView.Count == 1) && (h2oMonitorHourlyView.Count == 0) &&
                                ((h2oMonitorMethodView[0]["METHOD_CD"].AsString() == "MWD") ||
                                 (h2oMonitorMethodView[1]["METHOD_CD"].AsString() == "MWD")))
                            {
                                category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", "H2O_Derived_Hourly_Value_Records_By_Hour_Location", eParameterDataType.String);//Category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", H2oDerivedHourlyView, ParameterTypes.DATAVW);
                                category.SetCheckParameter("H2O_Reported_Value", h2oDerivedHourlyView[0]["Adjusted_Hrly_Value"].AsDecimal().Default(), eParameterDataType.Decimal);
                                category.SetCheckParameter("H2O_Method_Code", "MWD", eParameterDataType.String);
                            }
                            else if ((h2oDerivedHourlyView.Count == 0) && (h2oMonitorHourlyView.Count == 1) &&
                                     (((h2oMonitorMethodView[0]["METHOD_CD"].AsString().InList("MTB,MMS")) &&
                                        !h2oMonitorMethodView[1]["METHOD_CD"].AsString().InList("MTB,MMS")) ||
                                      ((h2oMonitorMethodView[1]["METHOD_CD"].AsString().InList("MTB,MMS")) &&
                                        !h2oMonitorMethodView[0]["METHOD_CD"].AsString().InList("MTB,MMS"))))
                            {
                                category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", "H2O_Monitor_Hourly_Value_Records_By_Hour_Location", eParameterDataType.String);//Category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", H2oDerivedHourlyView, ParameterTypes.DATAVW);
                                category.SetCheckParameter("H2O_Reported_Value", h2oMonitorHourlyView[0]["Unadjusted_Hrly_Value"].AsDecimal().Default(), eParameterDataType.Decimal);

                                if ((h2oMonitorMethodView[0]["METHOD_CD"].AsString() == "MMS") ||
                                    (h2oMonitorMethodView[1]["METHOD_CD"].AsString() == "MMS"))
                                    category.SetCheckParameter("H2O_Method_Code", "MMS", eParameterDataType.String);
                                else
                                    category.SetCheckParameter("H2O_Method_Code", "MTB", eParameterDataType.String);
                            }
                            else
                                category.CheckCatalogResult = "A";
                        }
                    }
                    else if (h2oMonitorMethodView.Count == 1)
                    {
                        string h2oMethodCd = cDBConvert.ToString(h2oMonitorMethodView[0]["Method_Cd"]);
                        category.SetCheckParameter("H2O_Method_Code", h2oMethodCd, eParameterDataType.String);

                        if (cDBConvert.ToString(h2oMonitorMethodView[0]["SUB_DATA_CD"]).StartsWith("FSP75"))
                        {
                            category.SetCheckParameter("H2O_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);
                        }

                        if (h2oMethodCd == "MDF")
                        {
                            DataView h2oMonitorDefaultView = (DataView)category.GetCheckParameter("H2O_Monitor_Default_Records_By_Hour_Location").ParameterValue;

                            if (h2oMonitorDefaultView.Count == 0)
                            {
                                category.CheckCatalogResult = "B";
                            }
                            else if (h2oMonitorDefaultView.Count > 1)
                            {
                                if (h2oDerivedHourlyView.Count == 1)
                                    category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", "H2O_Derived_Hourly_Value_Records_By_Hour_Location", eParameterDataType.String);//Category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", H2oDerivedHourlyView, ParameterTypes.DATAVW);

                                h2oMonitorDefaultView.Sort = "Default_Value DESC";

                                decimal maxDefVal = cDBConvert.ToDecimal(h2oMonitorDefaultView[0]["Default_Value"]);
                                category.SetCheckParameter("H2O_Default_Max_Value", maxDefVal, eParameterDataType.Decimal);

                                h2oMonitorDefaultView.Sort = "Default_Value ASC";

                                decimal minDefVal = cDBConvert.ToDecimal(h2oMonitorDefaultView[0]["Default_Value"]);
                                category.SetCheckParameter("H2O_Default_Min_Value", minDefVal, eParameterDataType.Decimal);

                                if (maxDefVal <= 0 || minDefVal <= 0 || maxDefVal >= 100 || minDefVal >= 100)
                                    category.CheckCatalogResult = "C";
                            }
                            else
                            {
                                decimal defVal = cDBConvert.ToDecimal(h2oMonitorDefaultView[0]["Default_Value"]);
                                category.SetCheckParameter("H2O_Default_Value", defVal, eParameterDataType.Decimal);

                                if (defVal <= 0 || 100 <= defVal)
                                    category.CheckCatalogResult = "C";
                            }
                        }
                        else if (h2oMethodCd == "MWD")
                        {
                            if (h2oDerivedHourlyView.Count == 1)
                            {
                                decimal h2oDerivedValue = cDBConvert.ToDecimal(h2oDerivedHourlyView[0]["Adjusted_Hrly_Value"]);

                                category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", "H2O_Derived_Hourly_Value_Records_By_Hour_Location", eParameterDataType.String);
                                /*KS*/
                                //Category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", H2oDerivedHourlyView, ParameterTypes.DATAVW);
                                category.SetCheckParameter("H2O_Reported_Value", h2oDerivedValue, eParameterDataType.Decimal);
                            }
                        }
                        else if (h2oMethodCd.InList("MMS,MTB"))
                        {
                            if (h2oMonitorHourlyView.Count == 1)
                            {
                                decimal h2oMonitorValue = cDBConvert.ToDecimal(h2oMonitorHourlyView[0]["Unadjusted_Hrly_Value"]);

                                category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", "H2O_Monitor_Hourly_Value_Records_By_Hour_Location", eParameterDataType.String);
                                /*KS*/
                                //Category.SetCheckParameter("Current_Hourly_H2O_Table_Reference", H2oMonitorHourlyView, ParameterTypes.DATAVW);
                                category.SetCheckParameter("H2O_Reported_Value", h2oMonitorValue, eParameterDataType.Decimal);
                            }
                        }
                    }
                }
                else
                    log = false;
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex, "HOUROP5");
            }

            return returnVal;
        }

        public  string HOUROP6(cCategory Category, ref bool Log)
        // Verify NOx Rate Monitor Method
        // Formerly Hourly-82
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("NOx_Rate_Bypass_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("NOx_Rate_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Current_Nox_Rate_Monitor_Method_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("Current_NOxR_Method_Code", null, eParameterDataType.String);

                    DataView MonitorMethodView = (DataView)Category.GetCheckParameter("NOxR_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    if (MonitorMethodView.Count > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (MonitorMethodView.Count == 1)
                        if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                            Category.CheckCatalogResult = "B";
                        else
                        {
                            int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();

                            Category.SetCheckParameter("Current_Nox_Rate_Monitor_Method_Record", MonitorMethodView[0], eParameterDataType.DataRowView);
                            Category.SetCheckParameter("Current_NOxR_Method_Code", cDBConvert.ToString(MonitorMethodView[0]["Method_Cd"]), eParameterDataType.String);
                            Category.SetCheckParameter("NOx_Rate_Bypass_Code", cDBConvert.ToString(MonitorMethodView[0]["BYPASS_APPROACH_CD"]), eParameterDataType.String);
                            if (cDBConvert.ToString(MonitorMethodView[0]["SUB_DATA_CD"]).StartsWith("FSP75"))
                                Category.SetCheckParameter("NOx_Rate_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);
                            if (Convert.ToBoolean(Category.GetCheckParameter("Current_Unit_Is_Arp").ParameterValue))
                                Category.AccumCheckAggregate("Expected_Summary_Value_Nox_Rate_Array", CurrentLocationPos, true, true);
                        }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP6");
            }

            return ReturnVal;
        }

        public  string HOUROP7(cCategory Category, ref bool Log)
        // Verify NOx Mass Monitor Method Record
        // Formerly Hourly-112
        {
            string ReturnVal = "";

            try
            {
                if (Convert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("Current_NOx_Mass_Monitor_Method_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("NOx_Mass_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("NOx_Mass_Monitor_Method_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("NOx_Mass_Bypass_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("NOx_Mass_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);

                    DataView NoxMonitorMethodView = (DataView)Category.GetCheckParameter("NOx_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    if (NoxMonitorMethodView.Count > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (NoxMonitorMethodView.Count == 1)
                    {
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                        string MethodCode = cDBConvert.ToString(NoxMonitorMethodView[0]["Method_Cd"]);

                        Category.SetCheckParameter("Current_NOx_Mass_Monitor_Method_Record", NoxMonitorMethodView[0], eParameterDataType.DataRowView);
                        Category.SetCheckParameter("NOx_Mass_Monitor_Method_Code", MethodCode, eParameterDataType.String);

                        if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null && MethodCode != "LME")
                            Category.CheckCatalogResult = "B";
                        else
                        {
                            Category.AccumCheckAggregate("Expected_Summary_Value_Nox_Mass_Array", CurrentLocationPos, true, true);
                            Category.SetCheckParameter("NOx_Mass_Bypass_Code", cDBConvert.ToString(NoxMonitorMethodView[0]["BYPASS_APPROACH_CD"]), eParameterDataType.String);

                            if (cDBConvert.ToString(NoxMonitorMethodView[0]["SUB_DATA_CD"]).StartsWith("FSP75"))
                                Category.SetCheckParameter("NOx_Mass_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);

                            if (MethodCode.InList("CEM,NOXR,CEMNOXR,AMS"))
                                Category.SetCheckParameter("NOx_Mass_Method_Active_For_Hour", true, eParameterDataType.Boolean);

                            if ((MethodCode == "LME") && Category.GetCheckParameter("Current_Unit_Is_Arp").ValueAsBool())
                                Category.AccumCheckAggregate("Expected_Summary_Value_Nox_Rate_Array", CurrentLocationPos, true, true);
                        }
                    }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP7");
            }

            return ReturnVal;
        }

        public  string HOUROP8(cCategory Category, ref bool Log)
        // Verify CO2 Method Active During Current Hour
        // Formerly Hourly-133
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("CO2_Monitor_Method_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("CO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("CO2_Method_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("CO2_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);

                    DataView MonitorMethodView = (DataView)Category.GetCheckParameter("CO2_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    int CO2MethodCount = MonitorMethodView.Count;

                    if (CO2MethodCount > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (CO2MethodCount == 1)
                    {
                        DataRowView MonitorMethodRow = (DataRowView)MonitorMethodView[0];
                        string MethodCd = cDBConvert.ToString(MonitorMethodRow["Method_Cd"]);
                        Category.SetCheckParameter("CO2_Method_Code", MethodCd, eParameterDataType.String);
                        Category.SetCheckParameter("CO2_Monitor_Method_Record", MonitorMethodRow, eParameterDataType.DataRowView);
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                        if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null && MethodCd != "LME")
                            Category.CheckCatalogResult = "B";
                        else
                        {
                            if (cDBConvert.ToString(MonitorMethodRow["SUB_DATA_CD"]).StartsWith("FSP75"))
                                Category.SetCheckParameter("CO2_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);
                            if (MethodCd == "CEM")
                                Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                            else
                              if (MethodCd == "AD")
                                Category.SetCheckParameter("CO2_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                            Category.AccumCheckAggregate("Expected_Summary_Value_Co2_Array", CurrentLocationPos, true, true);
                        }
                    }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP8");
            }

            return ReturnVal;
        }

        public  string HOUROP9(cCategory Category, ref bool Log)
        // Verify Heat Input Method Active During Current hour
        // Formerly Hourly-168
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("Heat_Input_Monitor_Method_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("Heat_Input_Method_Code", null, eParameterDataType.String);
                    Category.SetCheckParameter("Heat_Input_Fuel_Specific_Missing_Data", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);

                    DataView MonitorMethodView = (DataView)Category.GetCheckParameter("HI_Monitor_Method_Records_By_Hour_Location").ParameterValue;

                    int HIMethodCount = MonitorMethodView.Count;

                    if (HIMethodCount > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null && (HIMethodCount == 0 || cDBConvert.ToString(MonitorMethodView[0]["PARAMETER_CD"]) != "HIT"))
                        Category.CheckCatalogResult = "B";
                    else
                        if (HIMethodCount == 1)
                    {
                        DataRowView MonitorMethodRow = (DataRowView)MonitorMethodView[0];
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                        string MethodCd = cDBConvert.ToString(MonitorMethodRow["METHOD_CD"]);
                        Category.SetCheckParameter("Heat_Input_Monitor_Method_Record", MonitorMethodRow, eParameterDataType.DataRowView);
                        Category.SetCheckParameter("Heat_Input_Method_Code", MethodCd, eParameterDataType.String);
                        string SubDataCd = cDBConvert.ToString(MonitorMethodRow["SUB_DATA_CD"]);
                        Category.SetCheckParameter("LME_HI_Substitute_Data_Code", SubDataCd, eParameterDataType.String);

                        if (SubDataCd.StartsWith("FSP75"))
                            Category.SetCheckParameter("Heat_Input_Fuel_Specific_Missing_Data", true, eParameterDataType.Boolean);
                        if (MethodCd == "CEM")
                            Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        else
                            if (MethodCd.InList("AD,ADCALC"))
                            Category.SetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        if (MethodCd != "EXP")
                            Category.AccumCheckAggregate("Expected_Summary_Value_Hi_Array", CurrentLocationPos, true, true);
                        if (cDBConvert.ToString(MonitorMethodRow["PARAMETER_CD"]) == "HI")
                            Category.SetArrayParameter("Apportionment_HI_Method_Array", CurrentLocationPos, MethodCd);
                    }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP9");
            }

            return ReturnVal;
        }

        #endregion


        #region Public  Methods: Checks (11 - 20)

        public  string HOUROP12(cCategory Category, ref bool Log)
        // Derived Hourly H2O Counts Match Method Code
        // Formerly Hourly-99
        {
            string ReturnVal = "";

            try
            {
                int H2oDerivedHourlyCount = cDBConvert.ToInteger(Category.GetCheckParameter("H2O_Derived_Hourly_Count").ParameterValue);

                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Unit_Hourly_Operational_Status").ParameterValue))
                {
                    string H2oMethodCd = cDBConvert.ToString(Category.GetCheckParameter("H2O_Method_Code").ParameterValue);

                    if (H2oDerivedHourlyCount == 0)
                    {
                        if (H2oMethodCd == "MWD")
                            Category.CheckCatalogResult = "A";
                    }
                    else if (H2oDerivedHourlyCount > 0)
                    {
                        if (H2oMethodCd != "MWD")
                            Category.CheckCatalogResult = "B";
                    }
                }
                else
                {
                    if (H2oDerivedHourlyCount > 0)
                        Category.CheckCatalogResult = "C";
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP12");
            }

            return ReturnVal;
        }

        public  string HOUROP13(cCategory Category, ref bool Log)
        // Monitor Hourly H2O Counts Match Method Code
        // Formerly Hourly-100
        {
            string ReturnVal = "";

            try
            {
                int H2oMonitorHourlyCount = cDBConvert.ToInteger(Category.GetCheckParameter("H2O_Monitor_Hourly_Count").ParameterValue);

                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Unit_Hourly_Operational_Status").ParameterValue))
                {
                    string H2oMethodCd = cDBConvert.ToString(Category.GetCheckParameter("H2O_Method_Code").ParameterValue);

                    if (H2oMonitorHourlyCount == 0)
                    {
                        if ((H2oMethodCd == "MMS") || (H2oMethodCd == "MTB"))
                            Category.CheckCatalogResult = "A";
                    }
                    else if (H2oMonitorHourlyCount > 0)
                    {
                        if ((H2oMethodCd != "MMS") && (H2oMethodCd != "MTB"))
                            Category.CheckCatalogResult = "B";
                    }
                }
                else
                {
                    if (H2oMonitorHourlyCount > 0)
                        Category.CheckCatalogResult = "C";
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP13");
            }

            return ReturnVal;
        }

        public  string HOUROP14(cCategory Category, ref bool Log)
        // Validate NOx Mass Monitor Method Record
        // Formerly Hourly-118
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("NOx_Mass_Method_Active_For_Hour", false, eParameterDataType.Boolean);

                DataRowView NoxmMonitorMethodRow = (DataRowView)Category.GetCheckParameter("Current_NOx_Mass_Monitor_Method_Record").ParameterValue;

                if (NoxmMonitorMethodRow != null)
                {
                    string NoxmMonitorMethodCode = cDBConvert.ToString(NoxmMonitorMethodRow["Method_Cd"]);

                    if ((NoxmMonitorMethodCode == "CEM") || (NoxmMonitorMethodCode == "NOXR"))
                        Category.SetCheckParameter("NOx_Mass_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP14");
            }

            return ReturnVal;
        }

        public  string HOUROP17(cCategory Category, ref bool Log)
        // Verify Single SO2 Derived Hourly Data Record
        // Formerly Hourly-12
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    DataView So2DerivedHourlyValueView = (DataView)Category.GetCheckParameter("SO2_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                    DataRowView HourlyOpDataRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                    int DHVRecordsCount = So2DerivedHourlyValueView.Count;
                    string SO2MethCd = Convert.ToString(Category.GetCheckParameter("SO2_Method_Code").ParameterValue);

                    Category.SetCheckParameter("F23_Default_Value", null, eParameterDataType.Decimal);
                    Category.SetCheckParameter("F23_Default_Max_Value", null, eParameterDataType.Decimal);
                    Category.SetCheckParameter("F23_Default_Min_Value", null, eParameterDataType.Decimal);
                    Category.SetCheckParameter("Current_SO2_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                    Category.SetCheckParameter("SO2_Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2M_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_Derived_Hourly_Count", DHVRecordsCount, eParameterDataType.Integer);

                    if (cDBConvert.ToDecimal(HourlyOpDataRow["OP_TIME"]) > 0)
                    {
                        if (DHVRecordsCount == 0 && SO2MethCd != "")
                            if (SO2MethCd == "AD")
                            {
                                if (Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) +
                                    Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) > 0)
                                    Category.CheckCatalogResult = "A";
                            }
                            else
                                Category.CheckCatalogResult = "A";
                        else
                        {
                            int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                            if (DHVRecordsCount > 0 && SO2MethCd == "")
                            {
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "B";
                            }
                            else if (DHVRecordsCount > 1)
                            {
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "C";
                            }
                            else if (DHVRecordsCount > 0 && SO2MethCd == "AD" &&
                                 Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) +
                                 Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) == 0)
                            {
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "G";
                            }
                            else if (DHVRecordsCount == 1)
                            {
                                DateTime Date = Convert.ToDateTime(Category.GetCheckParameter("Current_Operating_Date").ParameterValue);
                                int Hour = Convert.ToInt16(Category.GetCheckParameter("Current_Operating_Hour").ParameterValue);
                                if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                                {
                                    if (SO2MethCd == "LME")
                                    {
                                        if (cDBConvert.ToString(So2DerivedHourlyValueView[0]["PARAMETER_CD"]) == "SO2M")
                                        {
                                            So2DerivedHourlyValueView = FindActiveRows(So2DerivedHourlyValueView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                            Category.SetCheckParameter("Current_SO2_Derived_Hourly_Record", So2DerivedHourlyValueView[0], eParameterDataType.DataRowView);
                                            Category.SetCheckParameter("SO2M_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                                        }
                                        else
                                        {
                                            Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.CheckCatalogResult = "H";
                                        }
                                    }
                                    else
                                    {
                                        Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                    }
                                }
                                else
                                {
                                    if (cDBConvert.ToString(So2DerivedHourlyValueView[0]["PARAMETER_CD"]) == "SO2M")
                                    {
                                        Category.SetArrayParameter("Rpt_Period_So2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_So2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.CheckCatalogResult = "H";
                                    }
                                    else
                                    {
                                        So2DerivedHourlyValueView = FindActiveRows(So2DerivedHourlyValueView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                        Category.SetCheckParameter("Current_SO2_Derived_Hourly_Record", So2DerivedHourlyValueView[0], eParameterDataType.DataRowView);
                                        Category.SetCheckParameter("SO2_Derived_Hourly_Checks_Needed", true, eParameterDataType.Boolean);

                                        DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                        if (SO2MethCd.InList("CEMF23,AMS"))
                                        {
                                            if (SO2MethCd == "CEMF23")
                                                Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                                            string FormID = cDBConvert.ToString(So2DerivedHourlyValueView[0]["FORMULA_IDENTIFIER"]);
                                            if (FormID != "")
                                            {
                                                sFilterPair[] MonFormRecsFilterA = new sFilterPair[1];
                                                MonFormRecsFilterA[0].Set("FORMULA_IDENTIFIER", FormID);

                                                DataView SO2FormRecs = FindRows(MonFormRecs, MonFormRecsFilterA);
                                                if (SO2FormRecs.Count > 0)
                                                    if (cDBConvert.ToString(SO2FormRecs[0]["PARAMETER_CD"]) == "SO2")
                                                    {
                                                        if (SO2MethCd == "CEMF23")
                                                        {
                                                            if (cDBConvert.ToString(SO2FormRecs[0]["EQUATION_CD"]) == "F-23")
                                                            {
                                                                Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                                                                Category.SetCheckParameter("SO2_F23_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                                                            }
                                                        }
                                                        else
                                                          if (SO2MethCd == "AMS")
                                                            if (cDBConvert.ToString(SO2FormRecs[0]["EQUATION_CD"]).InList("F-1,F-2"))
                                                            {
                                                                Category.SetCheckParameter("SO2_Method_Code", "CEM", eParameterDataType.String);
                                                                Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                                                            }
                                                    }
                                            }
                                        }
                                        if (Convert.ToBoolean(Category.GetCheckParameter("SO2_F23_Method_Active_For_Hour").ParameterValue))
                                        {
                                            DataView DefRecs = (DataView)Category.GetCheckParameter("F23_Monitor_Default_Records_by_Hour_Location").ParameterValue;

                                            int F23RecCt = DefRecs.Count;
                                            if (F23RecCt == 0)
                                                Category.CheckCatalogResult = "D";
                                            else
                                              if (F23RecCt > 1)
                                            {
                                                DefRecs.Sort = "DEFAULT_VALUE DESC";
                                                decimal MaxDefVal = cDBConvert.ToDecimal(DefRecs[0]["DEFAULT_VALUE"]);
                                                Category.SetCheckParameter("F23_Default_Max_Value", MaxDefVal, eParameterDataType.Decimal);
                                                DefRecs.Sort = "DEFAULT_VALUE";
                                                decimal MinDefVal = cDBConvert.ToDecimal(DefRecs[0]["DEFAULT_VALUE"]);
                                                Category.SetCheckParameter("F23_Default_Min_Value", MinDefVal, eParameterDataType.Decimal);
                                                if (MaxDefVal <= 0 || MinDefVal <= 0)
                                                    Category.CheckCatalogResult = "E";
                                            }
                                            else
                                            {
                                                decimal DefVal = cDBConvert.ToDecimal(DefRecs[0]["DEFAULT_VALUE"]);
                                                Category.SetCheckParameter("F23_Default_Value", DefVal, eParameterDataType.Decimal);
                                                if (DefVal <= 0)
                                                    Category.CheckCatalogResult = "E";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    if (DHVRecordsCount > 0)
                        Category.CheckCatalogResult = "F";
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP17");
            }

            return ReturnVal;
        }

        public  string HOUROP18(cCategory Category, ref bool Log)
        // Verify Single SO2 Concentration record
        // Formerly Hourly-18
        {
            string ReturnVal = "";

            try
            {
                bool So2CemMethodActive = Convert.ToBoolean(Category.GetCheckParameter("SO2_CEM_Method_Active_For_Hour").ParameterValue);
                bool UnitHourlyOperationalStatus = Convert.ToBoolean(Category.GetCheckParameter("Unit_Hourly_Operational_Status").ParameterValue);
                DataView So2MonitorHourlyValueView = (DataView)Category.GetCheckParameter("SO2_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;

                Category.SetCheckParameter("Current_SO2_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("SO2_Monitor_Hourly_Count", So2MonitorHourlyValueView.Count, eParameterDataType.Integer);

                if (UnitHourlyOperationalStatus)
                {
                    if (So2MonitorHourlyValueView.Count > 0 && !So2CemMethodActive && !emParams.MatsSo2cNeeded.Default(false))
                        Category.CheckCatalogResult = "A";
                    else if (So2MonitorHourlyValueView.Count > 1)
                        Category.CheckCatalogResult = "B";
                    else if (So2MonitorHourlyValueView.Count == 1)
                        Category.SetCheckParameter("Current_SO2_Monitor_Hourly_Record", So2MonitorHourlyValueView[0], eParameterDataType.DataRowView);
                }
                else if (So2MonitorHourlyValueView.Count > 0)
                    Category.CheckCatalogResult = "C";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP18");
            }

            return ReturnVal;
        }

        public  string HOUROP19(cCategory Category, ref bool Log)
        // Verify Single NOx Concentration Record
        // Formerly Hourly-84
        {
            string ReturnVal = "";

            try
            {
                DataView NoxcMonitorHourlyDataView = (DataView)Category.GetCheckParameter("NOxC_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;

                Category.SetCheckParameter("Current_Nox_Conc_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("NOxC_Monitor_Hourly_Count", NoxcMonitorHourlyDataView.Count, eParameterDataType.Integer);

                if (Convert.ToBoolean(Category.GetCheckParameter("Unit_Hourly_Operational_Status").ParameterValue))
                {
                    if (NoxcMonitorHourlyDataView.Count > 1)
                        Category.CheckCatalogResult = "A";
                    else
                      if (NoxcMonitorHourlyDataView.Count == 1)
                        if (Category.GetCheckParameter("NOx_Mass_Monitor_Method_Code").ValueAsString().InList("CEM,CEMNOXR,AMS") ||
                            Category.GetCheckParameter("Current_NOxR_Method_Code").ValueAsString().InList("CEM,AMS"))
                            Category.SetCheckParameter("Current_Nox_Conc_Monitor_Hourly_Record", NoxcMonitorHourlyDataView[0], eParameterDataType.DataRowView);
                        else
                            Category.CheckCatalogResult = "B";
                }
                else
                  if (NoxcMonitorHourlyDataView.Count > 0)
                    Category.CheckCatalogResult = "C";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP19");
            }

            return ReturnVal;
        }

        public  string HOUROP20(cCategory Category, ref bool Log)
        // Verify Single NOx Rate Derived Hourly Record
        // Formerly Hourly-85
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("Current_NoxR_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("NOxR_Derived_Hourly_Checks_Needed", null, eParameterDataType.Boolean);
                Category.SetCheckParameter("NoxR_Derived_Hourly_Count", null, eParameterDataType.Integer);
                emParams.NoxrHasMeasuredDhvModc = null;

                if (Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ValueAsBool())
                {
                    DataView NoxrDerivedHourlyValueView = Category.GetCheckParameter("NOxR_Derived_Hourly_Value_Records_By_Hour_Location").ValueAsDataView();
                    DataRowView HourlyOpDataRow = Category.GetCheckParameter("Current_Hourly_Op_Record").ValueAsDataRowView();

                    Category.SetCheckParameter("NOxR_Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("NoxR_Derived_Hourly_Count", NoxrDerivedHourlyValueView.Count, eParameterDataType.Integer);

                    if (cDBConvert.ToDecimal(HourlyOpDataRow["OP_TIME"]) > 0)
                    {
                        string CurrentNoxrMethodCode = Category.GetCheckParameter("Current_NOxR_Method_Code").ValueAsString();
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();

                        if (NoxrDerivedHourlyValueView.Count == 0 && CurrentNoxrMethodCode != "")
                            Category.CheckCatalogResult = "A";
                        else
                        {
                            if ((NoxrDerivedHourlyValueView.Count > 0) && CurrentNoxrMethodCode == "")
                            {
                                Category.SetArrayParameter("Rpt_Period_NOx_Rate_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_NOx_Rate_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "B";
                            }
                            else if (NoxrDerivedHourlyValueView.Count > 1)
                            {
                                Category.SetArrayParameter("Rpt_Period_NOx_Rate_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_NOx_Rate_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "C";
                            }
                            else if (NoxrDerivedHourlyValueView.Count == 1)
                            {
                                Category.SetCheckParameter("Current_NoxR_Derived_Hourly_Record", NoxrDerivedHourlyValueView[0], eParameterDataType.DataRowView);
                                Category.SetCheckParameter("NOxR_Derived_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                Category.SetArrayParameter("Apportionment_NOXR_Method_Array", CurrentLocationPos, CurrentNoxrMethodCode);
                                emParams.NoxrHasMeasuredDhvModc = (cDBConvert.ToString(NoxrDerivedHourlyValueView[0]["MODC_CD"]).InList("01,02,03,04,05,14,21,22,53,54 "));

                                if (CurrentNoxrMethodCode == "AMS")
                                {
                                    string FormID = cDBConvert.ToString(NoxrDerivedHourlyValueView[0]["FORMULA_IDENTIFIER"]);
                                    if (FormID == "")
                                    {
                                        if (cDBConvert.ToString(NoxrDerivedHourlyValueView[0]["MODC_CD"]) != "")
                                            Category.SetCheckParameter("Current_NOxR_Method_Code", "CEM", eParameterDataType.String);
                                    }
                                    else
                                    {
                                        DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                        sFilterPair[] MonFormRecsFilterA = new sFilterPair[1];
                                        MonFormRecsFilterA[0].Set("FORMULA_IDENTIFIER", FormID);
                                        DataView NOXRFormRecs = FindRows(MonFormRecs, MonFormRecsFilterA);
                                        if (NOXRFormRecs.Count > 0)
                                            if (cDBConvert.ToString(NOXRFormRecs[0]["PARAMETER_CD"]) == "NOXR" &&
                                                cDBConvert.ToString(NOXRFormRecs[0]["EQUATION_CD"]).InList("F-5,F-6,19-1,19-2,19-3,19-3D,19-4,19-5,19-5D,19-6,19-7,19-8,19-9"))
                                                Category.SetCheckParameter("Current_NOxR_Method_Code", "CEM", eParameterDataType.String);
                                    }
                                }
                            }
                        }
                    }
                    else
                      if (NoxrDerivedHourlyValueView.Count > 0)
                        Category.CheckCatalogResult = "D";
                }
                else Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP20");
            }

            return ReturnVal;
        }

        #endregion


        #region Public  Methods: Checks (21 - 30)

        public  string HOUROP21(cCategory Category, ref bool Log)
        // Verify Single NOx Mass Derived Hourly Record
        // Formerly Hourly-119
        {
            string ReturnVal = "";

            try
            {
                if (cDBConvert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("NOx_Mass_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("NOXM_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Current_NOx_Mass_Derived_Hourly_Record", null, eParameterDataType.DataRowView);

                    DataView NoxmDerivedHourlyView = (DataView)Category.GetCheckParameter("NOx_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                    DataRowView HourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                    bool NoxMassMethodActive = cDBConvert.ToBoolean(Category.GetCheckParameter("NOx_Mass_Method_Active_For_Hour").ParameterValue);
                    string NOxMassMethCd = Convert.ToString(Category.GetCheckParameter("NOx_Mass_Monitor_Method_Code").ParameterValue);

                    if (cDBConvert.ToDecimal(HourlyOpRow["Op_Time"]) > 0)
                    {
                        if (NoxmDerivedHourlyView.Count == 0 && (NoxMassMethodActive || NOxMassMethCd == "LME"))
                            Category.CheckCatalogResult = "A";
                        else
                        {
                            int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                            if (NoxmDerivedHourlyView.Count > 0 && !NoxMassMethodActive && NOxMassMethCd != "LME")
                            {
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "B";
                            }
                            else
                              if (NoxmDerivedHourlyView.Count > 1)
                            {
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "C";
                            }
                            else
                                if (NoxmDerivedHourlyView.Count > 0 && Category.GetCheckParameter("Current_NOxR_Method_Code").ValueAsString() == "AE" &&
                                     Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) +
                                     Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) == 0)
                            {
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "E";
                            }
                            else
                                  if (NoxmDerivedHourlyView.Count == 1)
                            {
                                DateTime Date = Convert.ToDateTime(Category.GetCheckParameter("Current_Operating_Date").ParameterValue);
                                int Hour = Convert.ToInt16(Category.GetCheckParameter("Current_Operating_Hour").ParameterValue);
                                if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                                {
                                    if (NOxMassMethCd == "LME")
                                    {
                                        if (cDBConvert.ToString(NoxmDerivedHourlyView[0]["PARAMETER_CD"]) == "NOXM")
                                        {
                                            NoxmDerivedHourlyView = FindActiveRows(NoxmDerivedHourlyView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                            Category.SetCheckParameter("Current_NOx_Mass_Derived_Hourly_Record", NoxmDerivedHourlyView[0], eParameterDataType.DataRowView);
                                            Category.SetCheckParameter("NOXM_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                                        }
                                        else
                                        {
                                            Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.CheckCatalogResult = "F";
                                        }
                                    }
                                    else
                                    {
                                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                    }
                                }
                                else
                                {
                                    if (cDBConvert.ToString(NoxmDerivedHourlyView[0]["PARAMETER_CD"]) == "NOXM")
                                    {
                                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_NOx_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.CheckCatalogResult = "F";
                                    }
                                    else
                                    {
                                        NoxmDerivedHourlyView = FindActiveRows(NoxmDerivedHourlyView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                        Category.SetCheckParameter("Current_NOx_Mass_Derived_Hourly_Record", NoxmDerivedHourlyView[0], eParameterDataType.DataRowView);
                                        Category.SetCheckParameter("NOx_Mass_Derived_Checks_Needed", true, eParameterDataType.Boolean);

                                        if (NOxMassMethCd.InList("AMS,CEMNOXR"))
                                        {
                                            if (Convert.ToInt16(Category.GetCheckParameter("NoxR_Derived_Hourly_Count").ParameterValue) > 0)
                                                Category.SetCheckParameter("NOx_Mass_Monitor_Method_Code", "NOXR", eParameterDataType.String);
                                            else
                                              if (NOxMassMethCd == "CEMNOXR")
                                                Category.SetCheckParameter("NOx_Mass_Monitor_Method_Code", "CEM", eParameterDataType.String);
                                            else
                                            {
                                                string FormId = cDBConvert.ToString(NoxmDerivedHourlyView[0]["FORMULA_IDENTIFIER"]);
                                                if (FormId != "")
                                                {
                                                    DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                                    sFilterPair[] MonFormRecsFilterA = new sFilterPair[1];
                                                    MonFormRecsFilterA[0].Set("FORMULA_IDENTIFIER", FormId);
                                                    DataView NOXRFormRecs = FindRows(MonFormRecs, MonFormRecsFilterA);
                                                    if (NOXRFormRecs.Count > 0)
                                                    {
                                                        string EquCd = cDBConvert.ToString(NOXRFormRecs[0]["EQUATION_CD"]);
                                                        if (cDBConvert.ToString(NOXRFormRecs[0]["PARAMETER_CD"]) == "NOX" && EquCd.InList("F-26A,F-26B"))
                                                            Category.SetCheckParameter("NOx_Mass_Monitor_Method_Code", "CEM", eParameterDataType.String);
                                                    }
                                                }
                                            }
                                        }
                                        Category.SetArrayParameter("Apportionment_NOX_Method_Array", CurrentLocationPos, Convert.ToString(Category.GetCheckParameter("NOx_Mass_Monitor_Method_Code").ParameterValue));
                                    }
                                }
                            }
                        }
                    }
                    else if (NoxmDerivedHourlyView.Count > 0)
                        Category.CheckCatalogResult = "D";
                }
                else Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP21");
            }

            return ReturnVal;
        }

        public  string HOUROP22(cCategory Category, ref bool Log)
        // Verify Single CO2 Mass Derived Hourly Value Record 
        // Formerly Hourly-135
        {
            string ReturnVal = "";

            try
            {
                if (Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ValueAsBool())
                {
                    Category.SetCheckParameter("CO2_Mass_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("CO2M_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Current_CO2_Mass_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                    DataView CO2MassDerivedHourlyView = Category.GetCheckParameter("CO2_Derived_Hourly_Records_By_Hour_Location").ValueAsDataView();

                    DataRowView HourlyOpDataRow = Category.GetCheckParameter("Current_Hourly_Op_Record").ValueAsDataRowView();

                    Category.SetCheckParameter("CO2_Mass_Derived_Hourly_Count", CO2MassDerivedHourlyView.Count, eParameterDataType.Integer);
                    string CO2MethCd = Convert.ToString(Category.GetCheckParameter("CO2_Method_Code").ParameterValue);
                    if (cDBConvert.ToDecimal(HourlyOpDataRow["OP_TIME"]) > 0)
                    {
                        if (CO2MassDerivedHourlyView.Count == 0 && CO2MethCd != "" && CO2MethCd != "FSA")
                            if (CO2MethCd == "AD")
                            {
                                if (Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) +
                                    Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) > 0)
                                    Category.CheckCatalogResult = "A";
                            }
                            else
                                Category.CheckCatalogResult = "A";
                        else
                        {
                            int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                            if (CO2MassDerivedHourlyView.Count > 0 && (CO2MethCd == "" || CO2MethCd == "FSA"))
                            {
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "B";
                            }
                            else
                            if (CO2MassDerivedHourlyView.Count > 1)
                            {
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "C";
                            }
                            else
                              if (CO2MassDerivedHourlyView.Count > 0 && CO2MethCd == "AD" &&
                                  Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) +
                                  Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) == 0)
                            {
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                Category.CheckCatalogResult = "E";
                            }
                            else
                                if (CO2MassDerivedHourlyView.Count == 1)
                            {
                                DateTime Date = Convert.ToDateTime(Category.GetCheckParameter("Current_Operating_Date").ParameterValue);
                                int Hour = Convert.ToInt16(Category.GetCheckParameter("Current_Operating_Hour").ParameterValue);
                                if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                                {
                                    if (CO2MethCd == "LME")
                                    {
                                        if (cDBConvert.ToString(CO2MassDerivedHourlyView[0]["PARAMETER_CD"]) == "CO2M")
                                        {
                                            CO2MassDerivedHourlyView = FindActiveRows(CO2MassDerivedHourlyView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                            Category.SetCheckParameter("Current_CO2_Mass_Derived_Hourly_Record", CO2MassDerivedHourlyView[0], eParameterDataType.DataRowView);
                                            Category.SetCheckParameter("CO2M_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                                        }
                                        else
                                        {
                                            Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                            Category.CheckCatalogResult = "F";
                                        }
                                    }
                                    else
                                    {
                                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                    }
                                }
                                else
                                {
                                    if (cDBConvert.ToString(CO2MassDerivedHourlyView[0]["PARAMETER_CD"]) == "CO2M")
                                    {
                                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.SetArrayParameter("Rpt_Period_CO2_Mass_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                        Category.CheckCatalogResult = "F";
                                    }
                                    else
                                    {
                                        CO2MassDerivedHourlyView = FindActiveRows(CO2MassDerivedHourlyView, Date, Hour, Date, Hour, "BEGIN_DATE", "BEGIN_HOUR", "BEGIN_DATE", "BEGIN_HOUR");
                                        Category.SetCheckParameter("Current_CO2_Mass_Derived_Hourly_Record", CO2MassDerivedHourlyView[0], eParameterDataType.DataRowView);
                                        Category.SetCheckParameter("CO2_Mass_Derived_Checks_Needed", true, eParameterDataType.Boolean);

                                        if (CO2MethCd == "AMS")
                                        {
                                            string FormId = cDBConvert.ToString(CO2MassDerivedHourlyView[0]["FORMULA_IDENTIFIER"]);
                                            if (FormId != "")
                                            {
                                                DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                                sFilterPair[] MonFormRecsFilter = new sFilterPair[1];
                                                MonFormRecsFilter[0].Set("FORMULA_IDENTIFIER", FormId);
                                                DataView CO2FormRecs = FindRows(MonFormRecs, MonFormRecsFilter);
                                                if (CO2FormRecs.Count > 0)
                                                {
                                                    string EquCd = cDBConvert.ToString(CO2FormRecs[0]["EQUATION_CD"]);
                                                    if (cDBConvert.ToString(CO2FormRecs[0]["PARAMETER_CD"]) == "CO2" && EquCd.InList("F-2,F-11"))
                                                    {
                                                        Category.SetCheckParameter("CO2_Method_Code", "CEM", eParameterDataType.Boolean);
                                                        Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                      if (CO2MassDerivedHourlyView.Count > 0)
                        Category.CheckCatalogResult = "D";
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP22");
            }

            return ReturnVal;
        }

        public  string HOUROP23(cCategory Category, ref bool Log)
        // Verify Single CO2 Conc Derived or Monitor Hourly Data Record
        // Formerly Hourly-141
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("Current_CO2_Conc_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_CO2_Conc_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_CO2_Conc_Missing_Data_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("CO2_Conc_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("CO2_Conc_Monitor_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Dry_Needed_To_Support_Co2_Calculation", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Wet_Needed_To_Support_Co2_Calculation", false, eParameterDataType.Boolean);
                emParams.Co2cHasMeasuredDhvModc = null;

                DataView CO2CDHVDataRecs = (DataView)Category.GetCheckParameter("CO2C_Derived_Hourly_Records_By_Hour_Location").ParameterValue;
                DataView CO2CMHVDataRecs = (DataView)Category.GetCheckParameter("CO2C_Monitor_Hourly_Records_By_Hour_Location").ParameterValue;
                DataRowView CurrentHourlyOpRec = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                int DHVRecsCt = CO2CDHVDataRecs.Count;
                int MHVRecsCt = CO2CMHVDataRecs.Count;
                Category.SetCheckParameter("CO2_Conc_Derived_Hourly_Count", DHVRecsCt, eParameterDataType.Integer);
                Category.SetCheckParameter("CO2_Conc_Monitor_Hourly_Count", MHVRecsCt, eParameterDataType.Integer);

                int TotalCO2ConcRecs = DHVRecsCt + MHVRecsCt;

                if (cDBConvert.ToDecimal(CurrentHourlyOpRec["OP_TIME"]) > 0)
                {
                    bool HINeeded = Convert.ToBoolean(Category.GetCheckParameter("CO2_Conc_Checks_Needed_for_Heat_Input").ParameterValue);
                    bool DiluentNeeded = Convert.ToBoolean(Category.GetCheckParameter("CO2_Diluent_Checks_Needed_For_Nox_Rate_Calc").ParameterValue);
                    bool MassNeeded = Convert.ToBoolean(Category.GetCheckParameter("CO2_Conc_Checks_Needed_For_CO2_Mass_Calc").ParameterValue);
                    bool MATSDiluentNeeded = emParams.Co2DiluentNeededForMats.Default(false);
                    if (HINeeded || DiluentNeeded || MassNeeded || MATSDiluentNeeded)
                        if (MHVRecsCt == 0 && (HINeeded || DiluentNeeded || MATSDiluentNeeded))
                        {
                            if (HINeeded || (DiluentNeeded && emParams.NoxrHasMeasuredDhvModc.Default(false)) || (MATSDiluentNeeded && emParams.Co2DiluentNeededForMatsCalculation == true))
                                Category.CheckCatalogResult = "B";
                            else
                                Category.CheckCatalogResult = "F";
                        }
                        else
              if (TotalCO2ConcRecs == 0)
                            Category.CheckCatalogResult = "A";
                        else
                                if (MHVRecsCt == 2 && DHVRecsCt == 0 && (DiluentNeeded || MATSDiluentNeeded) && (HINeeded || MassNeeded))
                        {
                            sFilterPair[] Filter1 = new sFilterPair[1];
                            Filter1[0].Set("MODC_CD", "01,02,03,04,53,54", eFilterPairStringCompare.InList);
                            DataView CO2CMHVDataRecsFiltered = FindRows(CO2CMHVDataRecs, Filter1);
                            if (CO2CMHVDataRecsFiltered.Count == 0)
                                Category.CheckCatalogResult = "C";
                            else
                            {
                                Category.SetCheckParameter("Current_CO2_Conc_Monitor_Hourly_Record", CO2CMHVDataRecsFiltered[0], eParameterDataType.DataRowView);
                                Filter1[0].Set("MODC_CD", "01,02,03,04,54", eFilterPairStringCompare.InList, true);
                                DataView CO2CMissDataMHVDataRecsFiltered = FindRows(CO2CMHVDataRecs, Filter1);
                                if (CO2CMissDataMHVDataRecsFiltered.Count == 0)
                                    Category.CheckCatalogResult = "C";
                                else
                                {
                                    Category.SetCheckParameter("CO2_Conc_Monitor_Checks_Needed", true, eParameterDataType.Boolean);
                                    Category.SetCheckParameter("Current_CO2_Conc_Missing_Data_Monitor_Hourly_Record", CO2CMissDataMHVDataRecsFiltered[0], eParameterDataType.DataRowView);
                                }
                            }
                        }
                        else
                  if (TotalCO2ConcRecs > 1)
                            Category.CheckCatalogResult = "C";
                        else
                    if (DHVRecsCt == 1)
                        {
                            Category.SetCheckParameter("CO2_Conc_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                            Category.SetCheckParameter("Current_CO2_Conc_Derived_Hourly_Record", CO2CDHVDataRecs[0], eParameterDataType.DataRowView);
                            emParams.Co2cHasMeasuredDhvModc = (cDBConvert.ToString(CO2CDHVDataRecs[0]["MODC_CD"]).InList("01,02,03,04,05,21,53,54"));

                            string FormId = cDBConvert.ToString(CO2CDHVDataRecs[0]["MON_FORM_ID"]);

                            if (cDBConvert.ToString(CO2CDHVDataRecs[0]["MODC_CD"]).InList("01,02,03,04,05,21,53,54"))
                            {
                                Category.SetCheckParameter("FC_Factor_Needed", true, eParameterDataType.Boolean);
                                Category.SetCheckParameter("FD_Factor_Needed", true, eParameterDataType.Boolean);
                            }

                            if (FormId != "")
                            {
                                DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                sFilterPair[] Filter2 = new sFilterPair[1];
                                Filter2[0].Set("MON_FORM_ID", FormId);
                                DataRowView FormRec;

                                if (FindRow(MonFormRecs, Filter2, out FormRec))
                                    if (cDBConvert.ToString(FormRec["PARAMETER_CD"]) == "CO2C")
                                    {
                                        string EquCd = cDBConvert.ToString(FormRec["EQUATION_CD"]);

                                        if (EquCd == "F-14A")
                                            Category.SetCheckParameter("O2_Dry_Needed_To_Support_Co2_Calculation", false, eParameterDataType.Boolean);
                                        else
                                          if (EquCd == "F-14B")
                                        {
                                            Category.SetCheckParameter("O2_Wet_Needed_To_Support_Co2_Calculation", false, eParameterDataType.Boolean);
                                            Category.SetCheckParameter("Moisture_Needed", true, eParameterDataType.Boolean);
                                        }
                                    }
                            }
                        }
                        else
                        {
                            if (MHVRecsCt == 1)
                            {
                                Category.SetCheckParameter("CO2_Conc_Monitor_Checks_Needed", true, eParameterDataType.Boolean);
                                Category.SetCheckParameter("Current_CO2_Conc_Monitor_Hourly_Record", CO2CMHVDataRecs[0], eParameterDataType.DataRowView);
                            }
                        }
                    else
            if (TotalCO2ConcRecs > 0)
                        Category.CheckCatalogResult = "D";
                }
                else
                if (TotalCO2ConcRecs > 0)
                    Category.CheckCatalogResult = "E";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP23");
            }
            return ReturnVal;
        }

        public  string HOUROP24(cCategory Category, ref bool Log)
        // Count Hourly Fuel Flow Records
        {
            string ReturnVal = "";

            try
            {
                if (Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ValueAsBool())
                {
                    int HourlyFuelFlowCountForOil = 0;
                    int HourlyFuelFlowCountForGas = 0;

                    bool AppendixDMethodActive = Category.GetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour").ValueAsBool() ||
                                                 Category.GetCheckParameter("CO2_App_D_Method_Active_For_Hour ").ValueAsBool() ||
                                                 Category.GetCheckParameter("SO2_App_D_Method_Active_For_Hour").ValueAsBool();

                    DataView HourlyFuelFlowRecords = Category.GetCheckParameter("Hourly_Fuel_Flow_Records_For_Hour_Location").ValueAsDataView();

                    bool NullFuelCdOrGroup = false;

                    foreach (DataRowView HourlyFuelFlowRow in HourlyFuelFlowRecords)
                    {
                        if ((HourlyFuelFlowRow["Fuel_Cd"] == DBNull.Value) ||
                            (HourlyFuelFlowRow["Fuel_Group_Cd"] == DBNull.Value))
                            NullFuelCdOrGroup = true;
                        else
                        {
                            string FuelGroupCd = cDBConvert.ToString(HourlyFuelFlowRow["Fuel_Group_Cd"]);

                            if (FuelGroupCd == "GAS")
                                HourlyFuelFlowCountForGas += 1;
                            else if (FuelGroupCd == "OIL")
                                HourlyFuelFlowCountForOil += 1;
                        }
                    }

                    if (NullFuelCdOrGroup)
                        Category.CheckCatalogResult = "D";
                    else
                    {
                        DataRowView CurrentHourlyOpRow = Category.GetCheckParameter("Current_Hourly_Op_Record").ValueAsDataRowView();

                        int HourlyFuelFlowCount = HourlyFuelFlowCountForOil + HourlyFuelFlowCountForGas;

                        if (cDBConvert.ToString(CurrentHourlyOpRow["LOCATION_NAME"]).StartsWith("CP"))
                        {
                            int CPFuelCount = Category.GetCheckParameter("CP_Fuel_Count").ValueAsInt();
                            Category.SetCheckParameter("CP_Fuel_Count", CPFuelCount + HourlyFuelFlowCount, eParameterDataType.Integer);
                        }

                        if (cDBConvert.ToDecimal(CurrentHourlyOpRow["Op_Time"]) == 0)
                        {
                            if (HourlyFuelFlowCount > 0)
                                Category.CheckCatalogResult = "A";
                        }
                        else
                        {
                            if (AppendixDMethodActive && HourlyFuelFlowCount == 0 && Category.GetCheckParameter("MP_Pipe_Config_for_Hourly_Checks").ParameterValue == null)
                                Category.CheckCatalogResult = "B";
                            else if (!AppendixDMethodActive && HourlyFuelFlowCount > 0)
                                Category.CheckCatalogResult = "C";
                        }
                    }
                    Category.SetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil", HourlyFuelFlowCountForOil, eParameterDataType.Integer);
                    Category.SetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas", HourlyFuelFlowCountForGas, eParameterDataType.Integer);
                    Category.SetCheckParameter("Appendix_D_Method_Active", AppendixDMethodActive, eParameterDataType.Boolean);
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP24");
            }

            return ReturnVal;
        }

        public  string HOUROP30(cCategory Category, ref bool Log)
        // Determine Load Based Status of Unit
        {
            string ReturnVal = "";

            try
            {
                DataRowView MonitorLocationRow = (DataRowView)Category.GetCheckParameter("Current_Monitor_Plan_Location_Record").ParameterValue;

                string LocationName = cDBConvert.ToString(MonitorLocationRow["Location_Name"]).PadRight(6);

                if (LocationName.Substring(0, 2).InList("CS,CP,MS,MP"))
                {
                    DataView UnitStackConfigurationRecords = (DataView)Category.GetCheckParameter("Unit_Stack_Configuration_Records_By_Hour_Location").ParameterValue;

                    if (UnitStackConfigurationRecords.Count > 0)
                    {
                        bool AllNonLoadBased = true;

                        foreach (DataRowView UnitStackConfigurationRow in UnitStackConfigurationRecords)
                        {
                            bool LoadBased = (cDBConvert.ToInteger(UnitStackConfigurationRow["Non_Load_Based_Ind"], 0) == 0);

                            if (LoadBased)
                                AllNonLoadBased = false;
                        }

                        Category.SetCheckParameter("Unit_Is_Load_Based", !AllNonLoadBased, eParameterDataType.Boolean);
                    }
                    else
                        Category.SetCheckParameter("Unit_Is_Load_Based", true, eParameterDataType.Boolean);
                }
                else
                {
                    bool LoadBased = (cDBConvert.ToInteger(MonitorLocationRow["Non_Load_Based_Ind"], 0) == 0);
                    Category.SetCheckParameter("Unit_Is_Load_Based", LoadBased, eParameterDataType.Boolean);
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP30");
            }
            return ReturnVal;
        }

        #endregion


        #region Public  Methods: Checks (31 - 40)

        public  string HOUROP32(cCategory Category, ref bool Log)
        // Perform Load Checks for Operating Hour
        {
            string ReturnVal = "";

            try
            {
                emParams.CurrentMaximumLoadValue = null;

                DataRowView CurrentHourOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;

                if (CurrentHourOpRow != null)
                {
                    int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                    bool UnitIsLoadBased = cDBConvert.ToBoolean(Category.GetCheckParameter("Unit_Is_Load_Based").ParameterValue);

                    int HourLoad = cDBConvert.ToInteger(CurrentHourOpRow["Hr_Load"]);
                    string LoadUom = cDBConvert.ToString(CurrentHourOpRow["Load_Uom_Cd"]);
                    decimal OpTime = cDBConvert.ToDecimal(CurrentHourOpRow["Op_Time"]);

                    Category.SetArrayParameter("Apportionment_Optime_Array", CurrentLocationPos, OpTime);
                    Category.SetArrayParameter("Apportionment_Load_Array", CurrentLocationPos, HourLoad);
                    string EntityType = Convert.ToString(Category.GetCheckParameter("Current_Entity_Type").ParameterValue);

                    if (UnitIsLoadBased && OpTime > 0)
                    {
                        if (HourLoad < 0)
                        {
                            if (EntityType == "Unit")
                                Category.SetCheckParameter("Unit_Loadtimesoptime_Accumulator", -1, eParameterDataType.Decimal);
                            else if (EntityType.InList("CP,MP"))
                                Category.SetCheckParameter("Pipe_LoadTimesOpTime_Accumulator", -1, eParameterDataType.Decimal);
                            else
                                Category.SetCheckParameter("Stack_Loadtimesoptime_Accumulator", -1, eParameterDataType.Decimal);

                            Category.CheckCatalogResult = "A";
                        }
                        else
                        {
                            string MPStackConfig = Category.GetCheckParameter("MP_Stack_Config_For_Hourly_Checks").ValueAsString();
                            if (MPStackConfig == "MS" &&
                                EntityType == "Unit")
                                Category.SetCheckParameter("MP_Unit_Load", HourLoad, eParameterDataType.Decimal);

                            if (EntityType == "Unit")
                            {
                                decimal UnitAccum = Convert.ToDecimal(Category.GetCheckParameter("Unit_Loadtimesoptime_Accumulator").ParameterValue);
                                if (UnitAccum >= 0)
                                    Category.SetCheckParameter("Unit_Loadtimesoptime_Accumulator", UnitAccum + (HourLoad * OpTime), eParameterDataType.Decimal);
                            }
                            else if (EntityType.InList("CP,MP"))
                            {
                                decimal PipeAccum = Convert.ToDecimal(Category.GetCheckParameter("Pipe_LoadTimesOpTime_Accumulator").ParameterValue);
                                if (PipeAccum >= 0)
                                    Category.SetCheckParameter("Pipe_LoadTimesOpTime_Accumulator", PipeAccum + (HourLoad * OpTime), eParameterDataType.Decimal);
                            }
                            else
                            {
                                decimal StackAccum = Convert.ToDecimal(Category.GetCheckParameter("Stack_Loadtimesoptime_Accumulator").ParameterValue);
                                if (StackAccum >= 0)
                                    Category.SetCheckParameter("Stack_Loadtimesoptime_Accumulator", StackAccum + (HourLoad * OpTime), eParameterDataType.Decimal);
                            }

                            if (!LoadUom.InList("MW,KLBHR,MMBTUHR"))
                            {
                                Category.SetCheckParameter("MP_Load_UOM", "INVALID", eParameterDataType.String);
                                Category.CheckCatalogResult = "B";
                            }
                            else
                            {
                                string UOM = Category.GetCheckParameter("MP_Load_UOM").ValueAsString();
                                string RecordUOM = cDBConvert.ToString(CurrentHourOpRow["LOAD_UOM_CD"]);
                                if (UOM != "" && !LoadUom.InList(RecordUOM + ",INVALID"))
                                {
                                    Category.SetCheckParameter("MP_Load_UOM", "INVALID", eParameterDataType.String);
                                    Category.CheckCatalogResult = "C";
                                }
                                else
                                {
                                    if (UOM == "")
                                        Category.SetCheckParameter("MP_Load_UOM", RecordUOM, eParameterDataType.String);
                                    DataView MonLoadRecs = Category.GetCheckParameter("Monitor_Load_Records_by_Hour_and_Location").ValueAsDataView();
                                    if (MonLoadRecs.Count == 1 && cDBConvert.ToDecimal(MonLoadRecs[0]["MAX_LOAD_VALUE"]) > 0)
                                    {
                                        if (RecordUOM == cDBConvert.ToString(MonLoadRecs[0]["MAX_LOAD_UOM_CD"]))
                                        {
                                            if (HourLoad > cDBConvert.ToDecimal(MonLoadRecs[0]["MAX_LOAD_VALUE"]))
                                            {
                                                if (HourLoad >= ((decimal)1.25 * cDBConvert.ToInteger(MonLoadRecs[0]["MAX_LOAD_VALUE"])))
                                                {
                                                    Category.CheckCatalogResult = "L";
                                                }
                                                else
                                                {
                                                    Category.CheckCatalogResult = "H";
                                                }
                                            }
                                            else
                                            {
                                                emParams.CurrentMaximumLoadValue = cDBConvert.ToInteger(MonLoadRecs[0]["MAX_LOAD_VALUE"]);
                                            }
                                        }
                                        else
                                            Category.CheckCatalogResult = "I";
                                    }
                                    else
                                        Category.CheckCatalogResult = "J";
                                }
                            }
                        }
                    }
                    else if (OpTime == 0)
                    {
                        if (HourLoad != int.MinValue)
                            Category.CheckCatalogResult = "D";
                        else if (LoadUom != "")
                            Category.CheckCatalogResult = "E";
                    }
                    else if (!UnitIsLoadBased)
                    {
                        if (HourLoad != int.MinValue)
                            Category.CheckCatalogResult = "F";
                        else if (LoadUom != "")
                            Category.CheckCatalogResult = "G";
                    }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP32");
            }
            return ReturnVal;
        }

        public  string HOUROP33(cCategory Category, ref bool Log)
        // Check reported Fuel Code for Operating Hour
        {
            string ReturnVal = "";

            try
            {
                DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;

                if (CurrentHourlyOpRow != null)
                {
                    bool FuelCdValNeeded = false;
                    DataRowView SO2Record = (DataRowView)Category.GetCheckParameter("Current_SO2_Monitor_Hourly_Record").ParameterValue;
                    DataRowView NOxCRecord = (DataRowView)Category.GetCheckParameter("Current_Nox_Conc_Monitor_Hourly_Record").ParameterValue;
                    DataRowView NOxRRecord = (DataRowView)Category.GetCheckParameter("Current_NoxR_Derived_Hourly_Record").ParameterValue;
                    string SO2BypassCd = Convert.ToString(Category.GetCheckParameter("SO2_Bypass_Code").ParameterValue);
                    string NOxMassBypassCd = Convert.ToString(Category.GetCheckParameter("NOx_Mass_Bypass_Code").ParameterValue);
                    string NOxRateBypassCd = Convert.ToString(Category.GetCheckParameter("NOx_Rate_Bypass_Code").ParameterValue);
                    string FuelCd = cDBConvert.ToString(CurrentHourlyOpRow["FUEL_CD"]);
                    if (Convert.ToBoolean(Category.GetCheckParameter("NOx_Rate_Fuel_Specific_Missing_Data").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("NOx_Mass_Fuel_Specific_Missing_Data").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("So2_Fuel_Specific_Missing_Data").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("CO2_Fuel_Specific_Missing_Data").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("Heat_Input_Fuel_Specific_Missing_Data").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("H2O_Fuel_Specific_Missing_Data").ParameterValue))
                        FuelCdValNeeded = true;
                    else
                    {
                        if (SO2Record != null && SO2BypassCd == "BYMAXFS")
                            if (cDBConvert.ToString(SO2Record["MODC_CD"]) == "23")
                                FuelCdValNeeded = true;
                        if (NOxCRecord != null && NOxMassBypassCd == "BYMAXFS")
                            if (cDBConvert.ToString(NOxCRecord["MODC_CD"]).InList("23,24"))
                                FuelCdValNeeded = true;
                        if (NOxRRecord != null && NOxRateBypassCd == "BYMAXFS")
                            if (cDBConvert.ToString(NOxRRecord["MODC_CD"]).InList("23,24"))
                                FuelCdValNeeded = true;
                    }
                    if (FuelCdValNeeded)
                    {
                        if (FuelCd == "")
                            if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)
                                Category.CheckCatalogResult = "A";
                            else
                            {
                                string FuelGrpCd = cDBConvert.ToString(CurrentHourlyOpRow["FUEL_GROUP_CD"]);
                                if (FuelCd == "NFS" || (FuelGrpCd == "COAL" && FuelCd != "C"))
                                    Category.CheckCatalogResult = "B";
                            }
                    }
                    else
                    if (FuelCd != "")
                        if (!"BYMAXFS".InList(SO2BypassCd + "," + NOxRateBypassCd + "," + NOxMassBypassCd))
                            Category.CheckCatalogResult = "C";
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP33");
            }
            return ReturnVal;
        }


        /// <summary>
        /// Validate Reported FC Factor
        /// 
        /// Uses cross-check value to ensure that FC Factor reported in Hourly Operating Data is within acceptable range
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP34(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.ValidFcFactorExists = false;

                decimal? fcFactor = emParams.CurrentHourlyOpRecord.FcFactor;

                if (fcFactor == null)
                {
                    if (emParams.FcFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                }
                else if (fcFactor <= 0.0m)
                {
                    if (emParams.FcFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                    else
                    {
                        category.CheckCatalogResult = "C";
                    }
                }
                else
                {
                    emParams.ValidFcFactorExists = true;

                    if (emParams.SpecialFuelBurned != true)
                    {
                        FcValidationInfo fcValidationInfo;

                        if (emParams.FcValicationSpansQuarter == true)
                        {
                            fcValidationInfo = emParams.FcValidationInfoByLocationArray[emParams.CurrentMonitorPlanLocationPostion.Value];
                        }
                        else
                        {
                            bool dslFound = false;
                            bool pngOrNngFound = false;
                            bool otherFound = false;

                            if (emParams.CurrentMonitorPlanLocationRecord.UnitId != null)
                            {
                                CheckDataView<VwMpLocationFuelRow> unitFuelRecords
                                    = emParams.FacilityUnitFuelRecords.FindActiveRows(emParams.CurrentOperatingDate.Value,
                                                                                          emParams.CurrentOperatingDate.Value,
                                                                                          new cFilterCondition("UNIT_ID",
                                                                                                               eFilterConditionRelativeCompare.Equals,
                                                                                                               emParams.CurrentMonitorPlanLocationRecord.UnitId.Value),
                                                                                          new cFilterCondition("INDICATOR_CD", "P,S", eFilterConditionStringCompare.InList));

                                foreach (VwMpLocationFuelRow unitFuelRecord in unitFuelRecords)
                                {
                                    if (unitFuelRecord.FuelCd == "NNG" || unitFuelRecord.FuelCd == "PNG")
                                    {
                                        pngOrNngFound = true;
                                    }
                                    else if (unitFuelRecord.FuelCd == "DSL")
                                    {
                                        dslFound = true;
                                    }
                                    else
                                    {
                                        otherFound = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                CheckDataView<VwMpUnitStackConfigurationRow> unitStackConfigurationRecords
                                    = emParams.EmUnitStackConfigurationRecords.FindActiveRows(emParams.CurrentOperatingDate.Value,
                                                                                                  emParams.CurrentOperatingDate.Value,
                                                                                                  new cFilterCondition("STACK_PIPE_ID", emParams.CurrentMonitorPlanLocationRecord.StackPipeId));

                                foreach (VwMpUnitStackConfigurationRow unitStackConfigurationRecord in unitStackConfigurationRecords)
                                {
                                    CheckDataView<VwMpLocationFuelRow> unitFuelRecords
                                        = emParams.FacilityUnitFuelRecords.FindActiveRows(emParams.CurrentOperatingDate.Value,
                                                                                              emParams.CurrentOperatingDate.Value,
                                                                                              new cFilterCondition("UNIT_ID",
                                                                                                                   eFilterConditionRelativeCompare.Equals,
                                                                                                                   unitStackConfigurationRecord.UnitId.Value),
                                                                                              new cFilterCondition("INDICATOR_CD", "P,S", eFilterConditionStringCompare.InList));

                                    foreach (VwMpLocationFuelRow unitFuelRecord in unitFuelRecords)
                                    {
                                        if (unitFuelRecord.FuelCd == "NNG" || unitFuelRecord.FuelCd == "PNG")
                                        {
                                            pngOrNngFound = true;
                                        }
                                        else if (unitFuelRecord.FuelCd == "DSL")
                                        {
                                            dslFound = true;
                                        }
                                        else
                                        {
                                            otherFound = true;
                                            break;
                                        }
                                    }

                                    if (otherFound)
                                    {
                                        break;
                                    }
                                }
                            }


                            // Initialize fcValidationInfo to the general Fc range.
                            {
                                FFactorRangeChecksRow fFactorRangeChecksRow = emParams.FFactorRangeCrossCheckTable.FindRow(new cFilterCondition("Factor", "FC"));

                                decimal minValue = (fFactorRangeChecksRow != null) ? fFactorRangeChecksRow.LowerValue.AsDecimal().Default(Decimal.MinValue) : Decimal.MinValue;
                                decimal maxValue = (fFactorRangeChecksRow != null) ? fFactorRangeChecksRow.UpperValue.AsDecimal().Default(Decimal.MaxValue) : Decimal.MaxValue;

                                fcValidationInfo = new FcValidationInfo(FcValidationInfo.NonFuelSpecificMinValue.Value, FcValidationInfo.NonFuelSpecificMaxValue.Value);
                            }


                            if (!otherFound)
                            {
                                FuelTypeRealityChecksForFcFactorRow fuelTypeRealityChecksForFcFactorRow;

                                if (pngOrNngFound)
                                {
                                    fuelTypeRealityChecksForFcFactorRow
                                        = emParams.FuelTypeRealityChecksForFcFactorCrossCheckTable.FindRow(new cFilterCondition("FuelType", "GAS"));

                                    if (fuelTypeRealityChecksForFcFactorRow != null)
                                    {
                                        fcValidationInfo.UpdateToFuelSpecificRange(fuelTypeRealityChecksForFcFactorRow.LowerValue.AsDecimal().Default(Decimal.MinValue),
                                                                                   fuelTypeRealityChecksForFcFactorRow.UpperValue.AsDecimal().Default(Decimal.MaxValue));
                                    }
                                }

                                if (dslFound)
                                {
                                    fuelTypeRealityChecksForFcFactorRow
                                        = emParams.FuelTypeRealityChecksForFcFactorCrossCheckTable.FindRow(new cFilterCondition("FuelType", "OIL"));

                                    if (fuelTypeRealityChecksForFcFactorRow != null)
                                    {
                                        fcValidationInfo.UpdateToFuelSpecificRange(fuelTypeRealityChecksForFcFactorRow.LowerValue.AsDecimal().Default(Decimal.MinValue),
                                                                                   fuelTypeRealityChecksForFcFactorRow.UpperValue.AsDecimal().Default(Decimal.MaxValue));
                                    }
                                }
                            }
                        }


                        emParams.FcFactorMinimum = fcValidationInfo.MinValue;
                        emParams.FcFactorMaximum = fcValidationInfo.MaxValue;


                        if ((fcFactor > emParams.FcFactorMaximum) || (fcFactor < emParams.FcFactorMinimum))
                        {
                            if (fcValidationInfo.IsFuelSpecific)
                            {
                                category.CheckCatalogResult = "E";
                            }
                            else if (emParams.FcFactorNeeded == true)
                            {
                                category.CheckCatalogResult = "B";
                            }
                            else
                            {
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
        /// Validate Reported FD Factor
        /// 
        /// Uses cross-check value to ensure that FD Factor reported in Hourly Operating Data is within acceptable range
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP35(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.ValidFdFactorExists = false;

                decimal? fdFactor = emParams.CurrentHourlyOpRecord.FdFactor;

                if (fdFactor == null)
                {
                    if (emParams.FdFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                }
                else if (fdFactor <= 0.0m)
                {
                    if (emParams.FdFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                    else
                    {
                        category.CheckCatalogResult = "C";
                    }
                }
                else
                {
                    emParams.ValidFdFactorExists = true;

                    if (emParams.SpecialFuelBurned != true)
                    {
                        FFactorRangeChecksRow fFactorRangeChecksRow = emParams.FFactorRangeCrossCheckTable.FindRow(new cFilterCondition("Factor", "FD"));

                        emParams.FdFactorMinimum = fFactorRangeChecksRow.LowerValue.AsDecimal();
                        emParams.FdFactorMaximum = fFactorRangeChecksRow.UpperValue.AsDecimal();

                        if ((fdFactor > emParams.FdFactorMaximum) || (fdFactor < emParams.FdFactorMinimum))
                        {
                            if (emParams.FdFactorNeeded == true)
                            {
                                category.CheckCatalogResult = "B";
                            }
                            else
                            {
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
        /// Validate Reported FW Factor
        /// 
        /// Uses cross-check value to ensure that FW Factor reported in Hourly Operating Data is within acceptable range
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP36(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.ValidFwFactorExists = false;

                decimal? fwFactor = emParams.CurrentHourlyOpRecord.FwFactor;

                if (fwFactor == null)
                {
                    if (emParams.FwFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                }
                else if (fwFactor <= 0.0m)
                {
                    if (emParams.FwFactorNeeded == true)
                    {
                        category.CheckCatalogResult = "A";
                    }
                    else
                    {
                        category.CheckCatalogResult = "C";
                    }
                }
                else
                {
                    emParams.ValidFwFactorExists = true;

                    if (emParams.SpecialFuelBurned != true)
                    {
                        FFactorRangeChecksRow fFactorRangeChecksRow = emParams.FFactorRangeCrossCheckTable.FindRow(new cFilterCondition("Factor", "FW"));

                        emParams.FwFactorMinimum = fFactorRangeChecksRow.LowerValue.AsDecimal();
                        emParams.FwFactorMaximum = fFactorRangeChecksRow.UpperValue.AsDecimal();

                        if ((fwFactor > emParams.FwFactorMaximum) || (fwFactor < emParams.FwFactorMinimum))
                        {
                            if (emParams.FwFactorNeeded == true)
                            {
                                category.CheckCatalogResult = "B";
                            }
                            else
                            {
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


        public  string HOUROP37(cCategory Category, ref bool Log)
        //Verify Single Heat Input Derived Hourly Record
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("Current_Heat_Input_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Heat_Input_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("HIT_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                DataView HIDerHrlyRecs = (DataView)Category.GetCheckParameter("HI_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                int HIRecsCt = HIDerHrlyRecs.Count;
                DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                string EntityType = Convert.ToString(Category.GetCheckParameter("Current_Entity_Type").ParameterValue);

                if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)
                {
                    string InpMethCd = Convert.ToString(Category.GetCheckParameter("Heat_Input_Method_Code").ParameterValue);
                    if (HIRecsCt == 0)
                    {
                        if (InpMethCd != "")
                        {
                            if (!InpMethCd.InList("EXP,LTFF"))
                                Category.CheckCatalogResult = "A";
                            else
                            if (InpMethCd == "LTFF" && EntityType == "Unit")
                                Category.CheckCatalogResult = "A";
                        }
                    }
                    else
                    {
                        int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();
                        if (HIRecsCt > 0 && (InpMethCd == "" || InpMethCd == "EXP" || (InpMethCd == "LTFF" && EntityType == "CP")))
                        {
                            Category.SetArrayParameter("Rpt_Period_Hi_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                            Category.SetArrayParameter("Rpt_Period_Hi_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                            Category.CheckCatalogResult = "B";
                        }
                        else
                          if (HIRecsCt > 1)
                        {
                            Category.SetArrayParameter("Rpt_Period_Hi_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                            Category.SetArrayParameter("Rpt_Period_Hi_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                            Category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            if (Category.GetCheckParameter("LME_HI_Method").ParameterValue != null)
                            {
                                if (cDBConvert.ToString(HIDerHrlyRecs[0]["PARAMETER_CD"]) == "HIT")
                                {
                                    Category.SetCheckParameter("Current_Heat_Input_Derived_Hourly_Record", HIDerHrlyRecs[0], eParameterDataType.DataRowView);
                                    Category.SetCheckParameter("HIT_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                                }
                                else
                                {
                                    Category.SetArrayParameter("Rpt_Period_Hi_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                    Category.SetArrayParameter("Rpt_Period_Hi_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                    Category.CheckCatalogResult = "E";
                                }
                            }
                            else
                            {
                                if (cDBConvert.ToString(HIDerHrlyRecs[0]["PARAMETER_CD"]) == "HIT")
                                {
                                    Category.SetArrayParameter("Rpt_Period_Hi_Calculated_Accumulator_Array", CurrentLocationPos, -1m);
                                    Category.SetArrayParameter("Rpt_Period_Hi_Reported_Accumulator_Array", CurrentLocationPos, -1m);
                                    Category.CheckCatalogResult = "E";
                                }
                                else
                                {
                                    Category.SetCheckParameter("Current_Heat_Input_Derived_Hourly_Record", HIDerHrlyRecs[0], eParameterDataType.DataRowView);
                                    Category.SetCheckParameter("Heat_Input_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                                    if (InpMethCd == "AMS")
                                    {
                                        string FormId = cDBConvert.ToString(HIDerHrlyRecs[0]["FORMULA_IDENTIFIER"]);
                                        if (FormId != "")
                                        {
                                            DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                            sFilterPair[] MonFormRecsFilter = new sFilterPair[1];
                                            MonFormRecsFilter[0].Set("FORMULA_IDENTIFIER", FormId);
                                            DataRowView MonFormRow;
                                            if (FindRow(MonFormRecs, MonFormRecsFilter, out MonFormRow))
                                                if (cDBConvert.ToString(MonFormRow["FORMULA_IDENTIFIER"]) == "HI" && cDBConvert.ToString(MonFormRow["EQUATION_CD"]).InList("F-15,F-16,F-17,F-18"))
                                                {
                                                    Category.SetCheckParameter("Heat_Input_Method_Code", "CEM", eParameterDataType.String);
                                                    Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                                                }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                  if (HIRecsCt > 0)
                    Category.CheckCatalogResult = "D";

            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP37");
            }
            return ReturnVal;
        }

        public  string HOUROP38(cCategory Category, ref bool Log)
        //Determine Fuel Type
        {
            string ReturnVal = "";

            try
            {
                if (Convert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                    if (CurrentHourlyOpRow["FC_FACTOR"] != DBNull.Value || CurrentHourlyOpRow["FD_FACTOR"] != DBNull.Value || CurrentHourlyOpRow["FW_FACTOR"] != DBNull.Value)
                        if (Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Gas").ParameterValue) + Convert.ToInt16(Category.GetCheckParameter("Hourly_Fuel_Flow_Count_For_Oil").ParameterValue) == 0)
                        {
                            string FuelCd = cDBConvert.ToString(CurrentHourlyOpRow["FUEL_CD"]);
                            if (FuelCd == "" || FuelCd == "MIX")
                            {
                                DataView UnitFuelRecs = (DataView)Category.GetCheckParameter("Fuel_Records_By_Hour_Location").ParameterValue;
                                sFilterPair[] Filter = new sFilterPair[1];
                                Filter[0].Set("FUEL_CD", "OOL,PRG,PRS,OGS", eFilterPairStringCompare.InList);
                                DataView UnitFuelRecsFiltered = FindRows(UnitFuelRecs, Filter);
                                if (UnitFuelRecsFiltered.Count > 0)
                                    Category.SetCheckParameter("Special_Fuel_Burned", true, eParameterDataType.Boolean);
                            }
                            else
                                if (cDBConvert.ToString(CurrentHourlyOpRow["UNIT_FUEL_CD"]).InList("OOL,PRG,PRS,OGS"))
                                Category.SetCheckParameter("Special_Fuel_Burned", true, eParameterDataType.Boolean);
                        }
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP38");
            }
            return ReturnVal;
        }

        public  string HOUROP39(cCategory Category, ref bool Log)
        //Verify Single H2O Conc Derived or Monitor Hourly Data Record
        {
            string ReturnVal = "";

            try
            {
                Category.SetCheckParameter("H2O_Derived_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("H2O_Monitor_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_H2O_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_H2O_Derived_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("O2_Wet_Checks_Needed_For_H2O", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("O2_Dry_Checks_Needed_For_H2O", false, eParameterDataType.Boolean);
                emParams.H2oHasMeasuredDhvModc = null;


                DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                int H2ODHCount = Convert.ToInt16(Category.GetCheckParameter("H2O_Derived_Hourly_Count").ParameterValue);
                int H2OMHCount = Convert.ToInt16(Category.GetCheckParameter("H2O_Monitor_Hourly_Count").ParameterValue);
                if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)
                {
                    if (Convert.ToBoolean(Category.GetCheckParameter("Moisture_Needed").ParameterValue))
                    {
                        string H2OMethCd = Convert.ToString(Category.GetCheckParameter("H2O_Method_Code").ParameterValue);
                        if (H2ODHCount + H2OMHCount == 0)
                        {
                            if (H2OMethCd == "MWD")
                                Category.CheckCatalogResult = "A";
                            else
                              if (H2OMethCd != "MDF")
                                Category.CheckCatalogResult = "B";
                            else
                                if (Category.GetCheckParameter("H2O_Default_Max_Value").ParameterValue != null)
                                Category.CheckCatalogResult = "C";
                        }
                        else
                          if (H2ODHCount > 1)
                            Category.CheckCatalogResult = "D";
                        else
                            if (H2OMHCount > 1)
                            Category.CheckCatalogResult = "E";
                        else
                                if (H2ODHCount == 1 && H2OMethCd.InList("MTB,MMS"))
                            Category.CheckCatalogResult = "F";
                        else
                                  if (H2OMHCount == 1 && H2OMethCd.InList("MWD,MDF"))
                            Category.CheckCatalogResult = "G";
                        else
                                  if (H2OMHCount == 1)
                        {
                            DataView H2oMonitorHourlyView = (DataView)Category.GetCheckParameter("H2O_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                            Category.SetCheckParameter("Current_H2O_Monitor_Hourly_Record", H2oMonitorHourlyView[0], eParameterDataType.DataRowView);
                            Category.SetCheckParameter("H2O_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                        }
                        else
                                    if (H2ODHCount == 1)
                        {
                            DataView H2oDerivedHourlyView = (DataView)Category.GetCheckParameter("H2O_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                            Category.SetCheckParameter("Current_H2O_Derived_Hourly_Record", H2oDerivedHourlyView[0], eParameterDataType.DataRowView);
                            emParams.H2oHasMeasuredDhvModc = (cDBConvert.ToString(H2oDerivedHourlyView[0]["MODC_CD"]).InList("01,02,03,04,05,21,53,54"));

                            Category.SetCheckParameter("H2O_Derived_Hourly_Checks_Needed", true, eParameterDataType.Boolean);

                            string FormID = cDBConvert.ToString(H2oDerivedHourlyView[0]["FORMULA_IDENTIFIER"]);

                            if (FormID != "")
                            {
                                DataRowView H2OFormRec;
                                DataView MonFormRecs = (DataView)Category.GetCheckParameter("Monitor_Formula_Records_By_Hour_Location").ParameterValue;
                                sFilterPair[] Filter = new sFilterPair[1];
                                Filter[0].Set("FORMULA_IDENTIFIER", FormID);
                                if (FindRow(MonFormRecs, Filter, out H2OFormRec))
                                    if (cDBConvert.ToString(H2OFormRec["PARAMETER_CD"]) == "H2O" && cDBConvert.ToString(H2OFormRec["EQUATION_CD"]).InList("F-31,M-1K"))
                                    {
                                        Category.SetCheckParameter("O2_Wet_Checks_Needed_For_H2O", true, eParameterDataType.Boolean);
                                        Category.SetCheckParameter("O2_Dry_Checks_Needed_For_H2O", true, eParameterDataType.Boolean);
                                    }
                            }
                        }
                    }
                }
                else
                  if (H2OMHCount + H2ODHCount > 0)
                    Category.CheckCatalogResult = "I";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP39");
            }
            return ReturnVal;
        }

        public  string HOUROP40(cCategory Category, ref bool Log)
        //Verify Single O2 Dry Monitor Hourly Value Record
        {
            string ReturnVal = "";

            try
            {
                DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                Category.SetCheckParameter("O2_Dry_Monitor_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_O2_Dry_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_O2_Dry_Missing_Data_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                int O2DryCount = Convert.ToInt16(Category.GetCheckParameter("O2_Dry_Monitor_Hourly_Count").ParameterValue);
                int O2NullCount = Convert.ToInt16(Category.GetCheckParameter("O2_Null_Monitor_Hourly_Count").ParameterValue);

                if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)
                    if (Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_for_Heat_Input").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Needed_To_Support_Co2_Calculation").ParameterValue) ||
                                  Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_H2O").ParameterValue) ||
                                  emParams.O2DryNeededForMats.Default(false) //Added 11/10/2014
                                  )
                        if (O2DryCount == 0 &&
                          (Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_for_Heat_Input").ParameterValue) ||
                          Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                          Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Needed_To_Support_Co2_Calculation").ParameterValue) ||
                                        Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_H2O").ParameterValue) ||
                                        emParams.O2WetNeededForMats.Default(false) //Added 11/10/2014
                                        )
                                    )
                        {
                            if (emParams.O2WetChecksNeededForHeatInput.Default(false) ||
                                emParams.O2WetChecksNeededForNoxRateCalc.Default(false) && emParams.NoxrHasMeasuredDhvModc == true ||
                                emParams.O2WetNeededToSupportCo2Calculation.Default(false) && emParams.Co2cHasMeasuredDhvModc == true ||
                                emParams.O2WetChecksNeededForH2o.Default(false) && emParams.H2oHasMeasuredDhvModc == true ||
                                emParams.O2WetNeededForMats.Default(false) && emParams.O2WetNeededForMatsCalculation == true)
                                Category.CheckCatalogResult = "A";
                            else
                                Category.CheckCatalogResult = "G";
                        }
                        else
                        {
                            if (O2DryCount + O2NullCount == 0)
                            {
                                if (emParams.O2DryChecksNeededForHeatInput.Default(false) ||
                                    emParams.O2DryChecksNeededForNoxRateCalc.Default(false) && emParams.NoxrHasMeasuredDhvModc == true ||
                                    emParams.O2DryNeededToSupportCo2Calculation.Default(false) && emParams.Co2cHasMeasuredDhvModc == true ||
                                    emParams.O2DryChecksNeededForH2o.Default(false) && emParams.H2oHasMeasuredDhvModc == true ||
                                    emParams.O2DryNeededForMats.Default(false) && emParams.O2DryNeededForMatsCalculation == true)
                                    Category.CheckCatalogResult = "B";
                                else
                                    Category.CheckCatalogResult = "H";
                            }
                            else
                              if (O2DryCount + O2NullCount > 2 || (O2DryCount + O2NullCount == 2 && Convert.ToInt16(Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Count").ParameterValue) + O2NullCount == 2))
                                Category.CheckCatalogResult = "C";
                            else
                            {
                                DataView O2DryMHVRecs = (DataView)Category.GetCheckParameter("O2_Dry_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                                DataView MHVRecsFiltered;
                                if (O2DryCount + O2NullCount == 2)
                                    if (Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_for_Heat_Input").ParameterValue) &&
                                        (Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                                                            Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_H20").ParameterValue) ||
                                                            emParams.O2DryNeededForMats.Default(false) //Added 11/10/2014
                                                            )
                                                        )
                                    {
                                        DataView O2NullMHVRecs = (DataView)Category.GetCheckParameter("O2_Null_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                                        //Find Current Dry MHV Record
                                        sFilterPair[] Filter = new sFilterPair[1];
                                        Filter[0].Set("MODC_CD", "01,02,03,04,53,54", eFilterPairStringCompare.InList);
                                        MHVRecsFiltered = FindRows(O2DryMHVRecs, Filter);
                                        if (MHVRecsFiltered.Count == 0)
                                        {
                                            MHVRecsFiltered = FindRows(O2NullMHVRecs, Filter);
                                            if (MHVRecsFiltered.Count == 0)
                                                Category.CheckCatalogResult = "C";
                                        }
                                        if (string.IsNullOrEmpty(Category.CheckCatalogResult))
                                        {
                                            //Set Current Dry MHV Record
                                            Category.SetCheckParameter("Current_O2_Dry_Monitor_Hourly_Record", MHVRecsFiltered[0], eParameterDataType.DataRowView);

                                            //Find Missing Data MHV Record
                                            Filter[0].Set("MODC_CD", "01,02,03,04,54", eFilterPairStringCompare.InList, true);
                                            MHVRecsFiltered = FindRows(O2DryMHVRecs, Filter);
                                            if (MHVRecsFiltered.Count == 0)
                                            {
                                                MHVRecsFiltered = FindRows(O2NullMHVRecs, Filter);
                                                if (MHVRecsFiltered.Count == 0)
                                                    Category.CheckCatalogResult = "C";
                                            }
                                            if (string.IsNullOrEmpty(Category.CheckCatalogResult))
                                            {
                                                //Set Missing Data MHV Record
                                                Category.SetCheckParameter("Current_O2_Dry_Missing_Data_Monitor_Hourly_Record", MHVRecsFiltered[0], eParameterDataType.DataRowView);

                                                //else
                                                Category.SetCheckParameter("O2_Dry_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                            }
                                        }
                                    }
                                    else
                                        Category.CheckCatalogResult = "C";
                                else
                                  if (O2DryCount == 1)
                                {
                                    Category.SetCheckParameter("O2_Dry_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                    Category.SetCheckParameter("Current_O2_Dry_Monitor_Hourly_Record", O2DryMHVRecs[0], eParameterDataType.DataRowView);
                                }
                                else
                                    if (O2NullCount == 1)
                                {
                                    Category.SetCheckParameter("O2_Dry_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                    Category.SetCheckParameter("Current_O2_Dry_Monitor_Hourly_Record", (DataRowView)Category.GetCheckParameter("Current_O2_Null_Monitor_Hourly_Record").ParameterValue, eParameterDataType.DataRowView);
                                }
                            }
                        }
                    else
                    {
                        if (O2DryCount > 0)
                            Category.CheckCatalogResult = "D";
                        else
                          if (O2NullCount > 0 && !Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_for_Heat_Input").ParameterValue) &&
                              !Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) &&
                              !Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Needed_To_Support_Co2_Calculation").ParameterValue) &&
                                            !Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_H2O").ParameterValue) &&
                                            emParams.O2WetNeededForMats.Default(false) == false  //Added 11/10/2014
                                            )
                            Category.CheckCatalogResult = "E";
                    }
                else
                  if (O2DryCount + O2NullCount + Convert.ToInt16(Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Count").ParameterValue) > 0)
                    Category.CheckCatalogResult = "F";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP40");
            }
            return ReturnVal;
        }

        #endregion


        #region Public  Methods: Checks (41 - 50)

        public  string HOUROP41(cCategory Category, ref bool Log)
        //Verify Single O2 Wet Monitor Hourly Value Record
        {
            string ReturnVal = "";

            try
            {
                DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                Category.SetCheckParameter("O2_Wet_Monitor_Hourly_Checks_Needed", false, eParameterDataType.Boolean);
                Category.SetCheckParameter("Current_O2_Wet_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                Category.SetCheckParameter("Current_O2_Wet_Missing_Data_Monitor_Hourly_Record", null, eParameterDataType.DataRowView);
                int O2WetCount = Convert.ToInt16(Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Count").ParameterValue);
                int O2NullCount = Convert.ToInt16(Category.GetCheckParameter("O2_Null_Monitor_Hourly_Count").ParameterValue);
                if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)

                    if (Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_for_Heat_Input").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                        Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Needed_To_Support_Co2_Calculation").ParameterValue) ||
                                  Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_H2O").ParameterValue) ||
                                  emParams.O2WetNeededForMats.Default(false) // Added 11/10/2014
                                  )

                        if (Convert.ToInt16(Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Count").ParameterValue) == 0 &&
                            (Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_for_Heat_Input").ParameterValue) ||
                            Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                            Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Needed_To_Support_Co2_Calculation").ParameterValue) ||
                                        Convert.ToBoolean(Category.GetCheckParameter("O2_Dry_Checks_Needed_For_H2O").ParameterValue) ||
                                        emParams.O2DryNeededForMats.Default(false) // Added 11/10/2014
                                        ))
                        {
                            if (emParams.O2DryChecksNeededForHeatInput.Default(false) ||
                                emParams.O2DryChecksNeededForNoxRateCalc.Default(false) && emParams.NoxrHasMeasuredDhvModc == true ||
                                emParams.O2DryNeededToSupportCo2Calculation.Default(false) && emParams.Co2cHasMeasuredDhvModc == true ||
                                emParams.O2DryChecksNeededForH2o.Default(false) && emParams.H2oHasMeasuredDhvModc == true ||
                                emParams.O2DryNeededForMats.Default(false) && emParams.O2DryNeededForMatsCalculation == true)
                                Category.CheckCatalogResult = "A";
                            else
                                Category.CheckCatalogResult = "E";
                        }
                        else
                        {
                            if (O2WetCount + O2NullCount == 0)
                            {
                                if (emParams.O2WetChecksNeededForHeatInput.Default(false) ||
                                    emParams.O2WetChecksNeededForNoxRateCalc.Default(false) && emParams.NoxrHasMeasuredDhvModc == true ||
                                    emParams.O2WetNeededToSupportCo2Calculation.Default(false) && emParams.Co2cHasMeasuredDhvModc == true ||
                                    emParams.O2WetChecksNeededForH2o.Default(false) && emParams.H2oHasMeasuredDhvModc == true ||
                                    emParams.O2WetNeededForMats.Default(false) && emParams.O2WetNeededForMatsCalculation == true)
                                    Category.CheckCatalogResult = "B";
                                else
                                    Category.CheckCatalogResult = "F";
                            }
                            else
                              if (O2WetCount + O2NullCount > 2)
                                Category.CheckCatalogResult = "C";
                            else
                            {
                                DataView O2WetMHVRecs = (DataView)Category.GetCheckParameter("O2_Wet_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                                DataView MHVRecsFiltered;
                                if (O2WetCount + O2NullCount == 2)
                                    if (Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_for_Heat_Input").ParameterValue) &&
                                        (Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_Nox_Rate_Calc").ParameterValue) ||
                                                            Convert.ToBoolean(Category.GetCheckParameter("O2_Wet_Checks_Needed_For_H20").ParameterValue) ||
                                                            emParams.O2WetNeededForMats.Default(false) // Added 11/10/2014
                                                            )
                                                        )
                                    {
                                        DataView O2NullMHVRecs = (DataView)Category.GetCheckParameter("O2_Null_Monitor_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                                        //Find Wet MHV Record
                                        sFilterPair[] Filter = new sFilterPair[1];
                                        Filter[0].Set("MODC_CD", "01,02,03,04,53,54", eFilterPairStringCompare.InList);
                                        MHVRecsFiltered = FindRows(O2WetMHVRecs, Filter);
                                        if (MHVRecsFiltered.Count == 0)
                                        {
                                            MHVRecsFiltered = FindRows(O2NullMHVRecs, Filter);
                                            if (MHVRecsFiltered.Count == 0)
                                                Category.CheckCatalogResult = "C";
                                        }
                                        if (string.IsNullOrEmpty(Category.CheckCatalogResult))
                                        {
                                            //Set Wet MHV Record
                                            Category.SetCheckParameter("Current_O2_Wet_Monitor_Hourly_Record", MHVRecsFiltered[0], eParameterDataType.DataRowView);
                                            //Find Missing MHV Record
                                            Filter[0].Set("MODC_CD", "01,02,03,04,54", eFilterPairStringCompare.InList, true);
                                            MHVRecsFiltered = FindRows(O2WetMHVRecs, Filter);
                                            if (MHVRecsFiltered.Count == 0)
                                            {
                                                MHVRecsFiltered = FindRows(O2NullMHVRecs, Filter);
                                                if (MHVRecsFiltered.Count == 0)
                                                    Category.CheckCatalogResult = "C";
                                            }
                                            if (string.IsNullOrEmpty(Category.CheckCatalogResult))
                                            {
                                                //Set Missing MHV Record
                                                Category.SetCheckParameter("Current_O2_Wet_Missing_Data_Monitor_Hourly_Record", MHVRecsFiltered[0], eParameterDataType.DataRowView);

                                                //else
                                                Category.SetCheckParameter("O2_Wet_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                            }
                                        }
                                    }
                                    else
                                        Category.CheckCatalogResult = "C";
                                else
                                  if (O2WetCount == 1)
                                {
                                    Category.SetCheckParameter("O2_Wet_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                    Category.SetCheckParameter("Current_O2_Wet_Monitor_Hourly_Record", O2WetMHVRecs[0], eParameterDataType.DataRowView);
                                }
                                else
                                    if (O2NullCount == 1)
                                {
                                    Category.SetCheckParameter("O2_Wet_Monitor_Hourly_Checks_Needed", true, eParameterDataType.Boolean);
                                    Category.SetCheckParameter("Current_O2_Wet_Monitor_Hourly_Record", (DataRowView)Category.GetCheckParameter("Current_O2_Null_Monitor_Hourly_Record").ParameterValue, eParameterDataType.DataRowView);
                                }
                            }
                        }
                    else
                      if (O2WetCount > 0)
                        Category.CheckCatalogResult = "D";
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP41");
            }
            return ReturnVal;
        }

        public  string HOUROP42(cCategory Category, ref bool Log)
        //Verify Single SO2R Derived Hourly Data Record
        {
            string ReturnVal = "";

            try
            {
                if (Convert.ToBoolean(Category.GetCheckParameter("Derived_Hourly_Checks_Needed").ParameterValue))
                {
                    Category.SetCheckParameter("SO2R_Derived_Checks_Needed", false, eParameterDataType.Boolean);
                    DataView SO2RDHVRecs = (DataView)Category.GetCheckParameter("SO2R_Derived_Hourly_Value_Records_By_Hour_Location").ParameterValue;
                    int SO2RDHVRecsCt = SO2RDHVRecs.Count;
                    Category.SetCheckParameter("SO2R_Derived_Hourly_Count", SO2RDHVRecsCt, eParameterDataType.Integer);
                    DataRowView CurrentHourlyOpRow = (DataRowView)Category.GetCheckParameter("Current_Hourly_Op_Record").ParameterValue;
                    if (cDBConvert.ToDecimal(CurrentHourlyOpRow["OP_TIME"]) > 0)
                    {
                        if (SO2RDHVRecsCt == 0 && Category.GetCheckParameter("F23_Default_Max_Value").ParameterValue != null)
                            Category.CheckCatalogResult = "A";
                        else
                          if (SO2RDHVRecsCt > 0 && !Convert.ToBoolean(Category.GetCheckParameter("SO2_F23_Method_Active_For_Hour").ParameterValue))
                            Category.CheckCatalogResult = "B";
                        else
                            if (SO2RDHVRecsCt > 1)
                            Category.CheckCatalogResult = "C";
                        else
                              if (SO2RDHVRecsCt == 1)
                        {
                            Category.SetCheckParameter("Current_SO2R_Derived_Hourly_Record", SO2RDHVRecs[0], eParameterDataType.DataRowView);
                            Category.SetCheckParameter("SO2R_Derived_Checks_Needed", true, eParameterDataType.Boolean);
                        }
                    }
                    else
                    if (SO2RDHVRecsCt > 0)
                        Category.CheckCatalogResult = "D";
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP42");
            }
            return ReturnVal;
        }

        /// <summary>
        /// Validate Single Stack Flow Record
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP43(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.CurrentFlowMonitorHourlyRecord = null;

                category.SetDecimalArrayParameter("Apportionment_Stack_Flow_Array", emParams.CurrentMonitorPlanLocationPostion.Value, null);


                if ((emParams.FlowMhvOptionallyAllowed == true) && (emParams.FlowMonitorHourlyCount > 0))
                {
                    emParams.FlowMonitorHourlyChecksNeeded = true;
                }

                if (emParams.FlowMonitorHourlyChecksNeeded == true)
                {
                    if (emParams.FlowMonitorHourlyCount == 0)
                    {
                        emParams.FlowMonitorHourlyChecksNeeded = false;
                        category.CheckCatalogResult = "A";
                    }
                    else if (emParams.FlowMonitorHourlyCount > 1)
                    {
                        category.CheckCatalogResult = "B";
                    }
                    else // assuming only one rec in view.
                    {
                        emParams.CurrentFlowMonitorHourlyRecord = emParams.FlowMonitorHourlyValueRecordsByHourLocation.GetRow(0);

                        category.SetDecimalArrayParameter("Apportionment_Stack_Flow_Array", emParams.CurrentMonitorPlanLocationPostion.Value,
                                                                                            emParams.CurrentFlowMonitorHourlyRecord.UnadjustedHrlyValue);
                    }
                }
                else
                {
                    if (emParams.FlowMonitorHourlyCount > 0)
                    {
                        category.CheckCatalogResult = "C";
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
        /// Check Reporting of Load Range and Common Stack Load Range
        /// 
        /// Setermines whether a value should exists in LoadRange or CommonStackLoadRange and produces a result
        /// if either value exists and should not or does not exist and should exist,
        /// 
        /// Output Parameters:
        /// 
        ///     CheckLoadRangeValue   : Indicates whether to perform checks on Hourly Op Data's LoadRange.
        ///     CheckCsLoadRangeValue : Indicates whether to perform checks on Hourly Op Data's CpmmpnStackLoadRange.
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP44(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.CheckLoadRangeValue = false;
                emParams.CheckCsLoadRangeValue = false;

                if ((emParams.DerivedHourlyChecksNeeded == true) && (emParams.UnitIsLoadBased == true))
                {
                    decimal? hourLoad = emParams.CurrentHourlyOpRecord.HrLoad;
                    decimal? loadRange = emParams.CurrentHourlyOpRecord.LoadRange;
                    int? commonStackLoadRange = emParams.CurrentHourlyOpRecord.CommonStackLoadRange;
                    string currentHEntityType = emParams.CurrentEntityType;

                    if ((emParams.CurrentHourlyOpRecord.OpTime > 0) && (hourLoad >= 0) && (emParams.LmeAnnual == false) && (emParams.LmeOs == false))
                    {
                        if ((emParams.FlowMonitorHourlyChecksNeeded == true) || (emParams.NoxConcNeededForNoxMassCalc == true) || (emParams.NoxrDerivedHourlyChecksNeeded == true) ||
                            (emParams.So2HpffExists == true) || (emParams.Co2HpffExists == true) || (emParams.HiHpffExists == true))
                        {
                            if ((loadRange == null) && (commonStackLoadRange == null))
                            {
                                category.CheckCatalogResult = "A";
                            }
                            else if (currentHEntityType == "CS")
                            {
                                if (loadRange != null)
                                {
                                    emParams.CheckLoadRangeValue = true;
                                }

                                if (commonStackLoadRange != null)
                                {
                                    if (emParams.FlowMonitorHourlyCount == 0)
                                    {
                                        category.CheckCatalogResult = "C";
                                    }
                                    else
                                        emParams.CheckCsLoadRangeValue = true;
                                }
                            }
                            else if (currentHEntityType == "CP")
                            {
                                if (loadRange != null)
                                {
                                    emParams.CheckLoadRangeValue = true;
                                }

                                if (commonStackLoadRange != null)
                                {
                                    if ((emParams.HourlyFuelFlowCountForOil.Value + emParams.HourlyFuelFlowCountForGas.Value) == 0)
                                    {
                                        category.CheckCatalogResult = "D";
                                    }
                                    else
                                        emParams.CheckCsLoadRangeValue = true;
                                }
                            }
                            else
                            {
                                if (loadRange != null)
                                {
                                    emParams.CheckLoadRangeValue = true;
                                }

                                if (commonStackLoadRange != null)
                                {
                                    category.CheckCatalogResult = "E";
                                }
                            }
                        }
                        else
                        {
                            if ((loadRange != null) || (commonStackLoadRange != null))
                            {
                                category.CheckCatalogResult = "F";
                            }
                        }
                    }
                    else
                    {
                        if ((loadRange != null) || (commonStackLoadRange != null))
                        {
                            category.CheckCatalogResult = "B";
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
        /// Check Reported Load Range Value
        /// 
        /// Determines whether Load Range matches the calculated value.  If the load is zero, the calculated value is 0 and if
        /// the load is equal to or grater than the Monitor Load Maximum Value, the calcualted value is 10.
        /// 
        /// Otherwise, the logic determines the load range for the load, determines the boundries of the bin, and then extends 
        /// the boundries by two units.  It then ensures that the reported load range inclusively falls within the extended 
        /// boundries.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP45(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CheckLoadRangeValue == true)
                {
                    decimal loadRange = emParams.CurrentHourlyOpRecord.LoadRange.Value;
                    int currentMaximumLoadValue = emParams.CurrentMaximumLoadValue.HasValue ? emParams.CurrentMaximumLoadValue.Value : 0;

                    if (loadRange == 0)
                    {
                        category.CheckCatalogResult = "A";
                    }

                    else if (emParams.CurrentHourlyOpRecord.HrLoad.HasValue && (currentMaximumLoadValue > 0))
                    {
                        decimal hourLoad = emParams.CurrentHourlyOpRecord.HrLoad.Value;

                        emParams.CalculatedLoadRange = (int)Math.Floor((10 * hourLoad / currentMaximumLoadValue) + 1);

                        if (hourLoad == 0)
                        {
                            if (loadRange != 1)
                            {
                                category.CheckCatalogResult = "B";
                            }
                        }
                        else if (hourLoad >= currentMaximumLoadValue)
                        {
                            if (loadRange != 10)
                            {
                                category.CheckCatalogResult = "C";
                            }
                        }

                        else
                        {
                            decimal binSize = (decimal)currentMaximumLoadValue / 10;

                            decimal lowRangeBoundry = binSize * (loadRange - 1);
                            decimal highRangeBoundry = binSize * loadRange;

                            if ((hourLoad < (lowRangeBoundry - 2)) || (hourLoad > (highRangeBoundry + 2)))
                            {
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
        /// Check Reported Common Stack Load Range Value
        /// 
        /// Determines whether Common Stack Load Range matches the calculated value.  If the load is zero, the calculated 
        /// value is 0 and if the load is equal to or grater than the Monitor Load Maximum Value, the calcualted value is 10.
        /// 
        /// Otherwise, the logic determines the common stack load range for the load, determines the boundries of the bin, and 
        /// then extends the boundries by two units.  It then ensures that the reported common stack load range inclusively 
        /// falls within the extended boundries.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP46(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CheckCsLoadRangeValue == true)
                {
                    int commonStackLoadRange = emParams.CurrentHourlyOpRecord.CommonStackLoadRange.Value;
                    int currentMaximumLoadValue = emParams.CurrentMaximumLoadValue.HasValue ? emParams.CurrentMaximumLoadValue.Value : 0;

                    if (commonStackLoadRange == 0)
                    {
                        category.CheckCatalogResult = "A";
                    }

                    else if (emParams.CurrentHourlyOpRecord.HrLoad.HasValue && (currentMaximumLoadValue > 0))
                    {
                        decimal hourLoad = emParams.CurrentHourlyOpRecord.HrLoad.Value;

                        emParams.CalculatedCsLoadRange = (int)Math.Floor((20 * hourLoad / currentMaximumLoadValue) + 1);

                        if (hourLoad == 0)
                        {
                            if (commonStackLoadRange != 1)
                            {
                                category.CheckCatalogResult = "B";
                            }
                        }
                        else if (hourLoad >= currentMaximumLoadValue)
                        {
                            if (commonStackLoadRange != 20)
                            {
                                category.CheckCatalogResult = "C";
                            }
                        }

                        else
                        {
                            decimal binSize = (decimal)currentMaximumLoadValue / 20;

                            decimal lowRangeBoundry = binSize * (commonStackLoadRange - 1);
                            decimal highRangeBoundry = binSize * commonStackLoadRange;

                            if ((hourLoad < (lowRangeBoundry - 2)) || (hourLoad > (highRangeBoundry + 2)))
                            {
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
        /// Updates the QA Certification Event Supplemental Data for event dates and conditional data begin dates that occur in the quarter of the emission report being evaluated.
        /// 
        /// Increament QaCertificationSupplementalData.QaCertEventQuarterOpDays by 1 when:
        /// 
        /// 1) An increament has not already occurred for the date of CurrentOperatingDatehour.
        /// 2) QaCertEventDatehour is in the same quarter as CurrentOperatingDatehour,
        /// 3) The date of QaCertEventDatehour is before the date of CurrentOperatingDatehour,
        /// 
        /// 
        /// Increament QaCertificationSupplementalData.ConditionalDataBeginQuarterOpHours by 1 when:
        /// 
        /// 1) An increament has not already occurred for CurrentOperatingDatehour.
        /// 2) ConditionalDataBeginDatehour is not null,
        /// 3) ConditionalDataBeginDatehour is in the same quarter as CurrentOperatingDatehour,
        /// 4) ConditionalDataBeginDatehour is before CurrentOperatingDatehour,
        /// 
        /// 
        /// Increament QaCertificationSupplementalData.ConditionalDataBeginQuarterOsHours by 1 when:
        /// 
        /// 1) An increament has not already occurred for CurrentOperatingDatehour.
        /// 2) ConditionalDataBeginDatehour is not null,
        /// 3) ConditionalDataBeginDatehour is in the same quarter as CurrentOperatingDatehour,
        /// 4) ConditionalDataBeginDatehour is before CurrentOperatingDatehour,
        /// 5) The month of CurrentOperatingDatehour is in May, June, July, August or September.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP47(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.DerivedHourlyChecksNeeded.Default(false) && (emParams.CurrentOperatingTime.Value > 0))
                {
                    foreach (QaCertificationSupplementalData qaCertificationSupplementalData in emParams.QaCertEventSuppDataDictionaryArray[emParams.CurrentMonitorPlanLocationPostion.Value].Values)
                    {
                        qaCertificationSupplementalData.IncreamentOperatingCounts(emParams.CurrentOperatingDatehour.Value);
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
        /// Updates the System Operating Supplemental Data for Part 75 monitored and derived hourly data, and MATS monitored hourly data.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP48(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.DerivedHourlyChecksNeeded.Default(false) && (emParams.CurrentOperatingTime.Value > 0))
                {
                    DateTime currentOperatingHour = emParams.CurrentOperatingDatehour.Value;
                    int currentReportingPeriod = emParams.CurrentReportingPeriod.Value;

                    Dictionary<string, SystemOperatingSupplementalData> supplementalDataDictionary = emParams.SystemOperatingSuppDataDictionaryArray[emParams.CurrentMonitorPlanLocationPostion.Value];

                    /* CurrentCo2ConcDerivedHourlyRecord */
                    if (emParams.CurrentCo2ConcDerivedHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentCo2ConcDerivedHourlyRecord.MonSysId, emParams.CurrentCo2ConcDerivedHourlyRecord.ModcCd, emParams.CurrentCo2ConcDerivedHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentCo2ConcMonitorHourlyRecord */
                    if (emParams.CurrentCo2ConcMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentCo2ConcMonitorHourlyRecord.MonSysId, emParams.CurrentCo2ConcMonitorHourlyRecord.ModcCd, emParams.CurrentCo2ConcMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentFlowMonitorHourlyRecord */
                    if (emParams.CurrentFlowMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentFlowMonitorHourlyRecord.MonSysId, emParams.CurrentFlowMonitorHourlyRecord.ModcCd, emParams.CurrentFlowMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentHeatInputDerivedHourlyRecord */
                    if (emParams.CurrentHeatInputDerivedHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentHeatInputDerivedHourlyRecord.MonSysId, emParams.CurrentHeatInputDerivedHourlyRecord.ModcCd, emParams.CurrentHeatInputDerivedHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentH2oDerivedHourlyRecord */
                    if (emParams.CurrentH2oDerivedHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentH2oDerivedHourlyRecord.MonSysId, emParams.CurrentH2oDerivedHourlyRecord.ModcCd, emParams.CurrentH2oDerivedHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentH2oMonitorHourlyRecord */
                    if (emParams.CurrentH2oMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentH2oMonitorHourlyRecord.MonSysId, emParams.CurrentH2oMonitorHourlyRecord.ModcCd, emParams.CurrentH2oMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentNoxConcMonitorHourlyRecord */
                    if (emParams.CurrentNoxConcMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentNoxConcMonitorHourlyRecord.MonSysId, emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd, emParams.CurrentNoxConcMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentNoxrDerivedHourlyRecord */
                    if (emParams.CurrentNoxrDerivedHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentNoxrDerivedHourlyRecord.MonSysId, emParams.CurrentNoxrDerivedHourlyRecord.ModcCd, emParams.CurrentNoxrDerivedHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentO2DryMonitorHourlyRecord */
                    if (emParams.CurrentO2DryMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentO2DryMonitorHourlyRecord.MonSysId, emParams.CurrentO2DryMonitorHourlyRecord.ModcCd, emParams.CurrentO2DryMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentO2WetMonitorHourlyRecord */
                    if (emParams.CurrentO2WetMonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentO2WetMonitorHourlyRecord.MonSysId, emParams.CurrentO2WetMonitorHourlyRecord.ModcCd, emParams.CurrentO2WetMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentSo2MonitorHourlyRecord */
                    if (emParams.CurrentSo2MonitorHourlyRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.CurrentSo2MonitorHourlyRecord.MonSysId, emParams.CurrentSo2MonitorHourlyRecord.ModcCd, emParams.CurrentSo2MonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHclcMhvRecord */
                    if (emParams.MatsHclcMhvRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.MatsHclcMhvRecord.MonSysId, emParams.MatsHclcMhvRecord.ModcCd, emParams.MatsHclcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHfcMhvRecord */
                    if (emParams.MatsHfcMhvRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.MatsHfcMhvRecord.MonSysId, emParams.MatsHfcMhvRecord.ModcCd, emParams.MatsHfcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHgcMhvRecord */
                    if (emParams.MatsHgcMhvRecord != null)
                    {
                        HOUROP48_SuppUpdate(currentReportingPeriod, emParams.MatsHgcMhvRecord.MonSysId, emParams.MatsHgcMhvRecord.ModcCd, emParams.MatsHgcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
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
        /// Performs the system operating supplemental data updates for values from an individual hourly record.
        /// </summary>
        /// <param name="rptPeriodId">The RPT_PEIROD_ID for the reporting period of the emission repor..</param>
        /// <param name="monSysId">The primary key to the MONITOR_SYSTEM table.</param>
        /// <param name="modcCd">The MODC code for the hourly record.</param>
        /// <param name="monLocId">The primary key to the MONITOR_LOCATION table.</param>
        /// <param name="currentOperatingHour">The current operating hour being processed.</param>
        /// <param name="supplementalDataDictionary">The system operating supplemental data dictionary for the current location.</param>
        public  void HOUROP48_SuppUpdate(int rptPeriodId, string monSysId, string modcCd, string monLocId, DateTime currentOperatingHour, Dictionary<string, SystemOperatingSupplementalData> supplementalDataDictionary)
        {
            if (monSysId.IsNotEmpty())
            {
                SystemOperatingSupplementalData supplementalDataRecord;

                if (supplementalDataDictionary.ContainsKey(monSysId))
                {
                    supplementalDataRecord = supplementalDataDictionary[monSysId];
                }
                else
                {
                    supplementalDataRecord = new SystemOperatingSupplementalData(rptPeriodId, monSysId, monLocId);
                    supplementalDataDictionary.Add(monSysId, supplementalDataRecord);
                }

                supplementalDataRecord.IncreamentForCurrentHour(currentOperatingHour, modcCd);


                if (emParams.QaCertEventSuppDataDictionaryBySystem != null &&
                    emParams.QaCertEventSuppDataDictionaryBySystem.ContainsKey(monSysId) &&
                    emParams.QaCertEventSuppDataDictionaryBySystem[monSysId] != null)
                {
                    foreach (QaCertificationSupplementalData qaCertificationSupplementalData in emParams.QaCertEventSuppDataDictionaryBySystem[monSysId])
                    {
                        qaCertificationSupplementalData.IncreamentSystemCounts(currentOperatingHour, modcCd);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the Component Operating Supplemental Data for Part 75 and MATS monitored hourly data.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP49(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.DerivedHourlyChecksNeeded.Default(false) && (emParams.CurrentOperatingTime.Value > 0))
                {
                    DateTime currentOperatingHour = emParams.CurrentOperatingDatehour.Value;
                    int currentReportingPeriod = emParams.CurrentReportingPeriod.Value;

                    Dictionary<string, ComponentOperatingSupplementalData> supplementalDataDictionary = emParams.ComponentOperatingSuppDataDictionaryArray[emParams.CurrentMonitorPlanLocationPostion.Value];

                    /* CurrentCo2ConcMonitorHourlyRecord */
                    if (emParams.CurrentCo2ConcMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentCo2ConcMonitorHourlyRecord.ComponentId, emParams.CurrentCo2ConcMonitorHourlyRecord.ModcCd, emParams.CurrentCo2ConcMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentFlowMonitorHourlyRecord */
                    if (emParams.CurrentFlowMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentFlowMonitorHourlyRecord.ComponentId, emParams.CurrentFlowMonitorHourlyRecord.ModcCd, emParams.CurrentFlowMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentH2oMonitorHourlyRecord */
                    if (emParams.CurrentH2oMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentH2oMonitorHourlyRecord.ComponentId, emParams.CurrentH2oMonitorHourlyRecord.ModcCd, emParams.CurrentH2oMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentNoxConcMonitorHourlyRecord */
                    if (emParams.CurrentNoxConcMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentNoxConcMonitorHourlyRecord.ComponentId, emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd, emParams.CurrentNoxConcMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentO2DryMonitorHourlyRecord */
                    if (emParams.CurrentO2DryMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentO2DryMonitorHourlyRecord.ComponentId, emParams.CurrentO2DryMonitorHourlyRecord.ModcCd, emParams.CurrentO2DryMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentO2WetMonitorHourlyRecord */
                    if (emParams.CurrentO2WetMonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentO2WetMonitorHourlyRecord.ComponentId, emParams.CurrentO2WetMonitorHourlyRecord.ModcCd, emParams.CurrentO2WetMonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* CurrentSo2MonitorHourlyRecord */
                    if (emParams.CurrentSo2MonitorHourlyRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.CurrentSo2MonitorHourlyRecord.ComponentId, emParams.CurrentSo2MonitorHourlyRecord.ModcCd, emParams.CurrentSo2MonitorHourlyRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHclcMhvRecord */
                    if (emParams.MatsHclcMhvRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.MatsHclcMhvRecord.ComponentId, emParams.MatsHclcMhvRecord.ModcCd, emParams.MatsHclcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHfcMhvRecord */
                    if (emParams.MatsHfcMhvRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.MatsHfcMhvRecord.ComponentId, emParams.MatsHfcMhvRecord.ModcCd, emParams.MatsHfcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
                    }

                    /* MatsHgcMhvRecord */
                    if (emParams.MatsHgcMhvRecord != null)
                    {
                        HOUROP49_SuppUpdate(currentReportingPeriod, emParams.MatsHgcMhvRecord.ComponentId, emParams.MatsHgcMhvRecord.ModcCd, emParams.MatsHgcMhvRecord.MonLocId, currentOperatingHour, supplementalDataDictionary);
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
        /// Performs the component operating supplemental data updates for values from an individual hourly record.
        /// </summary>
        /// <param name="rptPeriodId">The RPT_PEIROD_ID for the reporting period of the emission repor..</param>
        /// <param name="componentId">The primary key to the COMPONENT table.</param>
        /// <param name="modcCd">The MODC code for the hourly record.</param>
        /// <param name="monLocId">The primary key to the MON_LOC_ID table.</param>
        /// <param name="currentOperatingHour">The current operating hour being processed.</param>
        /// <param name="supplementalDataDictionary">The component operating supplemental data dictionary for the current location.</param>
        public  void HOUROP49_SuppUpdate(int rptPeriodId, string componentId, string modcCd, string monLocId, DateTime currentOperatingHour, Dictionary<string, ComponentOperatingSupplementalData> supplementalDataDictionary)
        {
            if (componentId.IsNotEmpty())
            {
                ComponentOperatingSupplementalData supplementalDataRecord;

                if (supplementalDataDictionary.ContainsKey(componentId))
                {
                    supplementalDataRecord = supplementalDataDictionary[componentId];
                }
                else
                {
                    supplementalDataRecord = new ComponentOperatingSupplementalData(rptPeriodId, componentId, monLocId);
                    supplementalDataDictionary.Add(componentId, supplementalDataRecord);
                }

                supplementalDataRecord.IncreamentForCurrentHour(currentOperatingHour, modcCd);


                if (emParams.QaCertEventSuppDataDictionaryByComponent != null &&
                    emParams.QaCertEventSuppDataDictionaryByComponent.ContainsKey(componentId) &&
                    emParams.QaCertEventSuppDataDictionaryByComponent[componentId] != null)
                {
                    foreach (QaCertificationSupplementalData qaCertificationSupplementalData in emParams.QaCertEventSuppDataDictionaryByComponent[componentId])
                    {
                        qaCertificationSupplementalData.IncreamentComponentCounts(currentOperatingHour, modcCd);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the System Operating Supplemental Data for Part 75 monitored and derived hourly data, and MATS monitored hourly data.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP50(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.DerivedHourlyChecksNeeded.Default(false) && (emParams.CurrentOperatingTime.Value > 0))
                {
                    bool includeComponent, includeSystem;

                    DateTime currentOperatingHour = emParams.CurrentOperatingDatehour.Value;
                    int currentReportingPeriod = emParams.CurrentReportingPeriod.Value;

                    Dictionary<string, LastQualityAssuredValueSupplementalData> supplementalDataDictionary = emParams.LastQualityAssuredValueSuppDataDictionaryArray[emParams.CurrentMonitorPlanLocationPostion.Value];

                    for (int choice = 0; choice < 3; choice++)
                    {
                        includeSystem = (choice == 1);
                        includeComponent = (choice == 2);

                        /* CurrentCo2ConcDerivedHourlyRecord */
                        if (emParams.CurrentCo2ConcDerivedHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.MonLocId,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.ParameterCd,
                                                null,
                                                eHourlyType.Derived,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.MonSysId,
                                                null,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentCo2ConcDerivedHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentCo2ConcMonitorHourlyRecord */
                        if (emParams.CurrentCo2ConcMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentCo2ConcMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentFlowMonitorHourlyRecord */
                        if (emParams.CurrentFlowMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentFlowMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentFlowMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentFlowMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentFlowMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentFlowMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentFlowMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentFlowMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentFlowMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentH2oDerivedHourlyRecord */
                        if (emParams.CurrentH2oDerivedHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentH2oDerivedHourlyRecord.MonLocId,
                                                emParams.CurrentH2oDerivedHourlyRecord.ParameterCd,
                                                null,
                                                eHourlyType.Derived,
                                                emParams.CurrentH2oDerivedHourlyRecord.MonSysId,
                                                null,
                                                emParams.CurrentH2oDerivedHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentH2oDerivedHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentH2oDerivedHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentH2oMonitorHourlyRecord */
                        if (emParams.CurrentH2oMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentH2oMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentH2oMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentH2oMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentH2oMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentH2oMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentH2oMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentH2oMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentH2oMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentNoxConcMonitorHourlyRecord */
                        if (emParams.CurrentNoxConcMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentNoxConcMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentNoxrDerivedHourlyRecord */
                        if (emParams.CurrentNoxrDerivedHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentNoxrDerivedHourlyRecord.MonLocId,
                                                emParams.CurrentNoxrDerivedHourlyRecord.ParameterCd,
                                                null,
                                                eHourlyType.Derived,
                                                emParams.CurrentNoxrDerivedHourlyRecord.MonSysId,
                                                null,
                                                emParams.CurrentNoxrDerivedHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentNoxrDerivedHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentNoxrDerivedHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentO2DryMonitorHourlyRecord */
                        if (emParams.CurrentO2DryMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentO2DryMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentO2DryMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentO2DryMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentO2DryMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentO2DryMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentO2DryMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentO2DryMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentO2DryMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentO2WetMonitorHourlyRecord */
                        if (emParams.CurrentO2WetMonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentO2WetMonitorHourlyRecord.MonLocId,
                                                emParams.CurrentO2WetMonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentO2WetMonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentO2WetMonitorHourlyRecord.MonSysId,
                                                emParams.CurrentO2WetMonitorHourlyRecord.ComponentId,
                                                emParams.CurrentO2WetMonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentO2WetMonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentO2WetMonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
                        }

                        /* CurrentSo2MonitorHourlyRecord */
                        if (emParams.CurrentSo2MonitorHourlyRecord != null)
                        {
                            HOUROP50_SuppUpdate(currentReportingPeriod,
                                                emParams.CurrentSo2MonitorHourlyRecord.MonLocId,
                                                emParams.CurrentSo2MonitorHourlyRecord.ParameterCd,
                                                emParams.CurrentSo2MonitorHourlyRecord.MoistureBasis,
                                                eHourlyType.Monitor,
                                                emParams.CurrentSo2MonitorHourlyRecord.MonSysId,
                                                emParams.CurrentSo2MonitorHourlyRecord.ComponentId,
                                                emParams.CurrentSo2MonitorHourlyRecord.ModcCd,
                                                currentOperatingHour,
                                                emParams.CurrentSo2MonitorHourlyRecord.UnadjustedHrlyValue,
                                                emParams.CurrentSo2MonitorHourlyRecord.AdjustedHrlyValue,
                                                includeSystem, includeComponent,
                                                supplementalDataDictionary);
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
        /// Performs the last quality assured value supplemental data updates for values from an individual hourly record.
        /// </summary>
        /// <param name="rptPeriodId">The RPT_PEIROD_ID for the reporting period of the emission report.</param>
        /// <param name="monLocId">The MON_LOC_ID for the current monitor location in the emission report.</param>
        /// <param name="parameterCd">The PARAMETER_CD of the hourly record.</param>
        /// <param name="moistureBasis">The MOISTURE_BASIS of the hourly record.</param>
        /// <param name="hourlyType">Indicates whether the hourly data is monitored or derived.</param>
        /// <param name="monSysId">The primary key to the MONITOR_SYSTEM table.</param>
        /// <param name="componentId">The primary key to the COMPONENT table.</param>
        /// <param name="modcCd">The MODC code for the hourly record.</param>
        /// <param name="unadjustedHourlyValue">The unadjusted hourly value for the current hourly record.</param>
        /// <param name="adjustedHourlyValue">The adjusted hourly value for the current hourly record.</param>
        /// <param name="currentOperatingHour">The current operating hour being processed.</param>
        /// <param name="includeSystem">The current operating hour being processed.</param>
        /// <param name="currentOperatingHour">Indicates whether to update the value for the specific system, if reported.</param>
        /// <param name="includeComponent">Indicates whether to update the value for the specific component, if reported.</param>
        public  void HOUROP50_SuppUpdate(int rptPeriodId, string monLocId, string parameterCd, string moistureBasis, eHourlyType hourlyType, string monSysId, string componentId,
                                                string modcCd, DateTime currentOperatingHour,
                                                decimal? unadjustedHourlyValue, decimal? adjustedHourlyValue,
                                                bool includeSystem, bool includeComponent,
                                                Dictionary<string, LastQualityAssuredValueSupplementalData> supplementalDataDictionary)
        {
            // Ensure that the MODC is a Quality Assured MODC
            if (modcCd.InList(LastQualityAssuredValueSupplementalData.QualityAssuredModcList))
            {
                // Only run body if system or component specific, or system specific and system was reported, or component specific and component was reported.
                if ((!includeSystem || (monSysId.IsNotEmpty())) && (!includeComponent || (componentId.IsNotEmpty())))
                {
                    LastQualityAssuredValueSupplementalData supplementalDataRecord;

                    string monSysTarget = (includeSystem ? monSysId : null);
                    string componentTarget = (includeComponent ? componentId : null);

                    string dictionaryKey = LastQualityAssuredValueSupplementalData.FormatKey(rptPeriodId, monLocId, parameterCd, moistureBasis, hourlyType, monSysTarget, componentTarget);

                    if (supplementalDataDictionary.ContainsKey(dictionaryKey))
                    {
                        supplementalDataRecord = supplementalDataDictionary[dictionaryKey];
                    }
                    else
                    {
                        supplementalDataRecord = new LastQualityAssuredValueSupplementalData(rptPeriodId, monLocId, parameterCd, moistureBasis, hourlyType, monSysTarget, componentTarget);
                        supplementalDataDictionary.Add(dictionaryKey, supplementalDataRecord);
                    }

                    supplementalDataRecord.UpdateLastQualityAssuredValue(currentOperatingHour, modcCd, unadjustedHourlyValue, adjustedHourlyValue);
                }
            }
        }

        #endregion


        #region Public  Methods: Checks (51 - 60)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP51(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.PrimaryBypassActiveForHour = false;
                emParams.PrimaryBypassActivePrimarySystemId = null;
                emParams.PrimaryBypassActiveBypassSystemId = null;

                if (emParams.PrimaryBypassActiveInQuarter == true)
                {
                    VwMpMonitorSystemRow monitorSystemRecord;

                    // Get Primary Bypass System for the hour
                    monitorSystemRecord = emParams.MonitorSystemRecordsByHourLocation.FindRow(new cFilterCondition("SYS_TYPE_CD", "NOX"), new cFilterCondition("SYS_DESIGNATION_CD", "PB"));

                    if (monitorSystemRecord != null)
                    {
                        emParams.PrimaryBypassActiveForHour = true;
                        emParams.PrimaryBypassActiveBypassSystemId = monitorSystemRecord.MonSysId;

                        // Get Primary System for the hour
                        monitorSystemRecord = emParams.MonitorSystemRecordsByHourLocation.FindRow(new cFilterCondition("SYS_TYPE_CD", "NOX"), new cFilterCondition("SYS_DESIGNATION_CD", "P"));

                        if (monitorSystemRecord != null)
                        {
                            emParams.PrimaryBypassActivePrimarySystemId = monitorSystemRecord.MonSysId;
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
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP52(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                decimal currentOpTime = emParams.CurrentHourlyOpRecord.OpTime.Value;

                if (emParams.DerivedHourlyChecksNeeded == true)
                {
                    Dictionary<string, decimal> systemOpTimeDictionary = new Dictionary<string, decimal>();

                    // Add systems to the system list if a primary bypass is active for the hour.
                    if (emParams.PrimaryBypassActiveForHour == true)
                    {
                        // Add used and maybe the unused NOx system if a NOx DHV exists with a P or PB system.
                        if ((emParams.CurrentNoxrDerivedHourlyRecord != null) && (emParams.CurrentNoxrDerivedHourlyRecord.MonSysId != null)
                                                                                  && (emParams.CurrentNoxrDerivedHourlyRecord.SysTypeCd == "NOX")
                                                                                  && (emParams.CurrentNoxrDerivedHourlyRecord.SysDesignationCd == "P" ||
                                                                                      emParams.CurrentNoxrDerivedHourlyRecord.SysDesignationCd == "PB"))
                        {
                            systemOpTimeDictionary.Add(emParams.CurrentNoxrDerivedHourlyRecord.MonSysId, currentOpTime);

                            foreach (NoxrPrimaryAndPrimaryBypassMhv mhvRecordsForUnusedNoxSystem in emParams.NoxrPrimaryOrPrimaryBypassMhvRecords)
                            {
                                if ((mhvRecordsForUnusedNoxSystem.NotReportedNoxrMonSysId != null) && !systemOpTimeDictionary.ContainsKey(mhvRecordsForUnusedNoxSystem.NotReportedNoxrMonSysId))
                                {
                                    systemOpTimeDictionary.Add(mhvRecordsForUnusedNoxSystem.NotReportedNoxrMonSysId, currentOpTime);
                                }
                            }
                        }


                        // Add Primary Bypass system id as not operating if it is not already in the system op time dictionary.
                        if (!systemOpTimeDictionary.ContainsKey(emParams.PrimaryBypassActiveBypassSystemId))
                        {
                            systemOpTimeDictionary.Add(emParams.PrimaryBypassActiveBypassSystemId, 0m);
                        }

                        // Add Primary system id as not operating if it is not already in the system op time dictionary.
                        if ((emParams.PrimaryBypassActivePrimarySystemId != null) && !systemOpTimeDictionary.ContainsKey(emParams.PrimaryBypassActivePrimarySystemId))
                        {
                            systemOpTimeDictionary.Add(emParams.PrimaryBypassActivePrimarySystemId, 0m);
                        }
                    }

                    // Update operating information for the location and if a primary bypass is active, for the primary and primary bypass systems.
                    emParams.MostRecentDailyCalibrationTestObject.UpdateOperatingInformation(emParams.CurrentHourlyOpRecord.MonLocId,
                                                                                                 emParams.CurrentHourlyOpRecord.BeginDatehour.Value,
                                                                                                 currentOpTime,
                                                                                                 systemOpTimeDictionary);
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Update Daily Interference Operating Information
        /// 
        /// Updates the operating hour count, last covered non-op hour, and first op hour after last covered 
        /// non--op hour information for daily interference tests tracked for status checking.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public  string HOUROP53(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                decimal currentOpTime = emParams.CurrentHourlyOpRecord.OpTime.Value;

                if (emParams.DerivedHourlyChecksNeeded == true)
                {
                    // Update operating information for the location and if a primary bypass is active, for the primary and primary bypass systems.
                    emParams.LatestDailyInterferenceCheckObject.UpdateOperatingInformation(emParams.CurrentMonitorLocationId,
                                                                                               emParams.CurrentOperatingDatehour.Value,
                                                                                               emParams.CurrentOperatingTime.Value);
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category">The category in which the check is running.</param>
        /// <param name="log">Obsolete.</param>
        /// <returns></returns>
        public  string HOUROP54(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.LinearityOfflineList = "";

                // Ensure that an issues does not exist with HRLY_OP_DATA for the hour and the location did not operate.
                if ((emParams.DerivedHourlyChecksNeeded == true) && (emParams.CurrentOperatingTime == 0.00m))
                {
                    // Quick check that any linearities exist for the location.
                    if (emParams.LinearityExistsLocationArray[emParams.CurrentMonitorPlanLocationPostion.Value])
                    {
                        // Check for linearities for the location where the begin or end hour equal the current hour.
                        foreach (VwQaSuppDataHourlyStatusRow linearitySuppDataRecord in emParams.LinearityTestRecordsByLocationForQaStatus)
                        {
                            if ((linearitySuppDataRecord.MonLocId == emParams.CurrentMonitorLocationId) &&
                                ((linearitySuppDataRecord.BeginDatehour == emParams.CurrentOperatingDatehour) ||
                                 (linearitySuppDataRecord.EndDatehour == emParams.CurrentOperatingDatehour)))
                            {
                                // Add linearities to the list of linearities during non operating hours.
                                emParams.LinearityOfflineList = emParams.LinearityOfflineList.ListAdd("'" + linearitySuppDataRecord.TestNum + "'");
                            }
                        }

                        // Return a result if any linearities during non-operating hours exist.
                        if (emParams.LinearityOfflineList != "")
                        {
                            emParams.LinearityOfflineList = emParams.LinearityOfflineList.FormatList(); // Format the list of linearities.
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
        /// Tracks Missing Data Counts and Last PMA for DHV CO2C, H2O and NOXR parameters, and MHV CO2C, FLOW, H2O, NOXC, O2D, O2W and SO2C parameters.
        /// </summary>
        /// <param name="category">The category in which the check is running.</param>
        /// <param name="log">Obsolete.</param>
        /// <returns></returns>
        public  string HOUROP55(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.DerivedHourlyChecksNeeded.Default(false) && (emParams.CurrentOperatingTime.Value > 0))
                {
                    MissingDataPmaTracking missingDataPmaTracking = emParams.MissingDataPmaTracking;
                    int locationPosition = emParams.CurrentMonitorPlanLocationPostion.Value;
                    DateTime opHour = emParams.CurrentOperatingDatehour.Value;
                    decimal opTime = emParams.CurrentOperatingTime.Value;


                    // Derived CO2C
                    if (emParams.CurrentCo2ConcDerivedHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                      MissingDataPmaTracking.eHourlyParameter.DerivedCo2c,
                                                      emParams.CurrentCo2ConcDerivedHourlyRecord.ModcCd,
                                                      emParams.CurrentCo2ConcDerivedHourlyRecord.PctAvailable);
                    }

                    // Derived H2O
                    if (emParams.CurrentH2oDerivedHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.DerivedH2o,
                                                  emParams.CurrentH2oDerivedHourlyRecord.ModcCd,
                                                  emParams.CurrentH2oDerivedHourlyRecord.PctAvailable);
                    }

                    // Derived NOXR
                    if (emParams.CurrentNoxrDerivedHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.DerivedNoxr,
                                                  emParams.CurrentNoxrDerivedHourlyRecord.ModcCd,
                                                  emParams.CurrentNoxrDerivedHourlyRecord.PctAvailable);
                    }

                    // Monitor CO2C
                    if (emParams.CurrentCo2ConcMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorCo2c,
                                                  emParams.CurrentCo2ConcMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentCo2ConcMonitorHourlyRecord.PctAvailable);
                    }

                    if (emParams.CurrentCo2ConcMissingDataMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorCo2c,
                                                  emParams.CurrentCo2ConcMissingDataMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentCo2ConcMissingDataMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor FLOW
                    if (emParams.CurrentFlowMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorFlow,
                                                  emParams.CurrentFlowMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentFlowMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor H2O
                    if (emParams.CurrentH2oMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorH2o,
                                                  emParams.CurrentH2oMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentH2oMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor NOXC
                    if (emParams.CurrentNoxConcMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorNoxc,
                                                  emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentNoxConcMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor O2C Dry
                    if (emParams.CurrentO2DryMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorO2d,
                                                  emParams.CurrentO2DryMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentO2DryMonitorHourlyRecord.PctAvailable);
                    }

                    if (emParams.CurrentO2DryMissingDataMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorO2d,
                                                  emParams.CurrentO2DryMissingDataMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentO2DryMissingDataMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor O2C Wet
                    if (emParams.CurrentO2WetMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorO2w,
                                                  emParams.CurrentO2WetMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentO2WetMonitorHourlyRecord.PctAvailable);
                    }

                    if (emParams.CurrentO2WetMissingDataMonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorO2w,
                                                  emParams.CurrentO2WetMissingDataMonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentO2WetMissingDataMonitorHourlyRecord.PctAvailable);
                    }

                    // Monitor SO2C
                    if (emParams.CurrentSo2MonitorHourlyRecord != null)
                    {
                        missingDataPmaTracking.Update(locationPosition, opHour, opTime,
                                                  MissingDataPmaTracking.eHourlyParameter.MonitorSo2c,
                                                  emParams.CurrentSo2MonitorHourlyRecord.ModcCd,
                                                  emParams.CurrentSo2MonitorHourlyRecord.PctAvailable);
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
        /// HOUROP-56
        /// 
        /// Ensure Consistent Reporting of MODC 46 for NOxC and Diluent Emissions for a NOxR System
        /// 
        /// Ensures that when MODC 46 is reported in a NOxC or Diluent MHV record when the/a Diluent is only needed for NOxR calculations, 
        /// that both the NOxC and Diluent MHV records must report MODC 46.
        /// </summary>
        /// <param name="category">The category in which the check is running.</param>
        /// <param name="log">Obsolete.</param>
        /// <returns></returns>
        public  string HOUROP56(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.MissingModc46ParameterForModc46 = null;
                emParams.MissingModc46ParameterForNon46 = null;
                emParams.MissingModc46Non46ModcCode = null;

                if ((emParams.DerivedHourlyChecksNeeded == true) && (emParams.CurrentOperatingTime > 0m))
                {
                    if (((emParams.NoxConcNeededForNoxRateCalc == true) && (emParams.CurrentNoxConcMonitorHourlyRecord != null)) &&
                        (((emParams.Co2DiluentChecksNeededForNoxRateCalc == true) && (emParams.CurrentCo2ConcMonitorHourlyRecord != null)) ||
                         ((emParams.O2DryChecksNeededForNoxRateCalc == true) && (emParams.CurrentO2DryMonitorHourlyRecord != null)) ||
                         ((emParams.O2WetChecksNeededForNoxRateCalc == true) && (emParams.CurrentO2WetMonitorHourlyRecord != null))))
                    {

                        if ((emParams.Co2ConcChecksNeededForCo2MassCalc != true) &&
                            (emParams.Co2ConcChecksNeededForHeatInput != true) &&
                            (emParams.Co2DiluentNeededForMats != true) &&
                            (emParams.O2DryChecksNeededForH2o != true) &&
                            (emParams.O2DryChecksNeededForHeatInput != true) &&
                            (emParams.O2DryNeededForMats != true) &&
                            (emParams.O2DryNeededToSupportCo2Calculation != true) &&
                            (emParams.O2WetChecksNeededForH2o != true) &&
                            (emParams.O2WetChecksNeededForHeatInput != true) &&
                            (emParams.O2WetNeededForMats != true) &&
                            (emParams.O2WetNeededToSupportCo2Calculation != true))
                        {
                            bool dilentRecordIsNull; string diluentModcCode, diluentParameter;
                            {
                                if ((emParams.Co2DiluentChecksNeededForNoxRateCalc == true) && (emParams.CurrentCo2ConcMonitorHourlyRecord != null))
                                {
                                    dilentRecordIsNull = (emParams.CurrentCo2ConcMonitorHourlyRecord == null);
                                    diluentModcCode = !dilentRecordIsNull ? emParams.CurrentCo2ConcMonitorHourlyRecord.ModcCd : null;
                                    diluentParameter = "CO2 Concentration";
                                }
                                else if ((emParams.O2DryChecksNeededForNoxRateCalc == true) && (emParams.CurrentO2DryMonitorHourlyRecord != null))
                                {
                                    dilentRecordIsNull = (emParams.CurrentO2DryMonitorHourlyRecord == null);
                                    diluentModcCode = !dilentRecordIsNull ? emParams.CurrentO2DryMonitorHourlyRecord.ModcCd : null;
                                    diluentParameter = "O2 Dry";
                                }
                                else // Otherwise ((emParams.O2WetChecksNeededForNoxRateCalc == true) && (emParams.CurrentO2WetMonitorHourlyRecord != null))
                                {
                                    dilentRecordIsNull = (emParams.CurrentO2WetMonitorHourlyRecord == null);
                                    diluentModcCode = !dilentRecordIsNull ? emParams.CurrentO2WetMonitorHourlyRecord.ModcCd : null;
                                    diluentParameter = "O2 Wet";
                                }
                            }


                            if ((emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd == "46") && (diluentModcCode != "46"))
                            {
                                emParams.MissingModc46ParameterForModc46 = "NOx Concentration";
                                emParams.MissingModc46ParameterForNon46 = diluentParameter;
                                emParams.MissingModc46Non46ModcCode = diluentModcCode;

                                category.CheckCatalogResult = "A";
                            }

                            else if (!dilentRecordIsNull && (diluentModcCode == "46") &&
                                ((emParams.CurrentNoxConcMonitorHourlyRecord == null) || (emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd != "46")))
                            {
                                emParams.MissingModc46ParameterForModc46 = diluentParameter;
                                emParams.MissingModc46ParameterForNon46 = "NOx Concentration";
                                emParams.MissingModc46Non46ModcCode = emParams.CurrentNoxConcMonitorHourlyRecord.ModcCd;

                                category.CheckCatalogResult = "A";
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


        #region Public  Methods: Cancelled

        public  string HOUROP10(cCategory Category, ref bool Log)
        // Determine Current SO2 Monitoring Method
        // Formerly Hourly-52
        {
            string ReturnVal = "";

            try
            {
                DataRowView So2MonitorMethodRecord = (DataRowView)Category.GetCheckParameter("Current_So2_Monitor_Method_Record").ParameterValue;

                if (So2MonitorMethodRecord != null)
                {
                    if (cDBConvert.ToString(So2MonitorMethodRecord["Method_Cd"]) == "CEM")
                    {
                        Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        Category.SetCheckParameter("SO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    }
                    else if (cDBConvert.ToString(So2MonitorMethodRecord["Method_Cd"]) == "AD")
                    {
                        Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                        Category.SetCheckParameter("SO2_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                    }
                }
                else
                {
                    Category.SetCheckParameter("SO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("SO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                }

            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP10");
            }

            return ReturnVal;
        }

        public  string HOUROP11(cCategory Category, ref bool Log)
        // Determine Current NOx Rate Monitoring Method Type
        // Formerly Hourly-83
        {
            string ReturnVal = "";

            try
            {
                DataRowView NoxRMonitorMethodRow = (DataRowView)Category.GetCheckParameter("Current_Nox_Rate_Monitor_Method_Record").ParameterValue;

                if (NoxRMonitorMethodRow != null)
                {
                    string NoxRMethodCode = cDBConvert.ToString(NoxRMonitorMethodRow["Method_Cd"]);

                    Category.SetCheckParameter("Current_NOxR_Method_Code", NoxRMethodCode, eParameterDataType.String);
                }
                else Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP11");
            }

            return ReturnVal;
        }

        public  string HOUROP15(cCategory Category, ref bool Log)
        // Determine Current CO2 Monitoring Method
        // Formerly Hourly-134
        {
            string ReturnVal = "";

            try
            {
                DataRowView CO2MonitorMethodRecord = (DataRowView)Category.GetCheckParameter("CO2_Monitor_Method_Record").ParameterValue;
                if (CO2MonitorMethodRecord != null)
                {
                    string MethodCode = cDBConvert.ToString(CO2MonitorMethodRecord["method_cd"]);
                    if (MethodCode == "CEM")
                    {
                        Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        Category.SetCheckParameter("CO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    }
                    else if (MethodCode == "AD")
                    {
                        Category.SetCheckParameter("CO2_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    }
                }
                else
                {
                    Category.SetCheckParameter("CO2_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("CO2_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP15");
            }

            return ReturnVal;
        }

        public  string HOUROP16(cCategory Category, ref bool Log)
        // Determine Current Heat Input Monitor Method
        // Formerly Hourly-166
        {
            string ReturnVal = "";

            try
            {
                DataRowView HIMonitorMethodRecord = (DataRowView)Category.GetCheckParameter("Heat_Input_Monitor_Method_Record").ParameterValue;

                if (HIMonitorMethodRecord != null)
                {
                    if (cDBConvert.ToString(HIMonitorMethodRecord["Method_Cd"]) == "CEM")
                    {
                        Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                        Category.SetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    }
                    else if (cDBConvert.ToString(HIMonitorMethodRecord["Method_Cd"]) == "AD")
                    {
                        Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                        Category.SetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour", true, eParameterDataType.Boolean);
                    }
                }
                else
                {
                    Category.SetCheckParameter("Heat_Input_CEM_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                    Category.SetCheckParameter("Heat_Input_App_D_Method_Active_For_Hour", false, eParameterDataType.Boolean);
                }
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP16");
            }

            return ReturnVal;
        }

        public  string HOUROP26(cCategory Category, ref bool Log)
        // (old) Detect Appendix E Reporting Method
        {
            string ReturnVal = "";

            try
            {
                DataRowView MonitorMethodRow = (DataRowView)Category.GetCheckParameter("Current_Nox_Rate_Monitor_Method_Record").ParameterValue;

                if (MonitorMethodRow != null)
                {
                    int CurrentLocationPos = Category.GetCheckParameter("Current_Monitor_Plan_Location_Postion").ValueAsInt();

                    string MethodCd = cDBConvert.ToString(MonitorMethodRow["Method_Cd"]);

                    Category.SetCheckParameter("Current_NOxR_Method_Code", MethodCd, eParameterDataType.String);
                    Category.AccumCheckAggregate("Expected_Summary_Value_Nox_Rate_Array", CurrentLocationPos, true, true);
                }
                else
                    Log = false;
            }
            catch (Exception ex)
            {
                ReturnVal = Category.CheckEngine.FormatError(ex, "HOUROP26");
            }

            return ReturnVal;
        }

        #endregion

    }
}