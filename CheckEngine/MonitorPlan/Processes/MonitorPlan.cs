using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.Definitions;
using ECMPS.Checks.DatabaseAccess;
using ECMPS.Checks.MonitorPlan;
using ECMPS.Checks.Mp.Parameters;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.MonitorPlanEvaluation
{
    public class cMonitorPlan : cMpProcess
    {

        #region Constructors

        public cMonitorPlan(cCheckEngine CheckEngine)
          : base(CheckEngine)
        {
            Console.WriteLine("*****************************************************Executing Monitoring Plan");
        }

        #endregion


        #region Base Class Overrides

        /// <summary>
        /// Loads the Check Procedure delegates needed for a process code.
        /// </summary>
        /// <param name="checksDllPath">The path of the checks DLLs.</param>
        /// <param name="errorMessage">The message returned if the initialization fails.</param>
        /// <returns>True if the initialization succeeds.</returns>
        public override bool CheckProcedureInit(string checksDllPath, ref string errorMessage)
        {
            bool result;

            try
            {
                Checks[06] = InstantiateChecks("cMonitorPlanChecks", "MonitorPlanChecks", checksDllPath, false);
                Checks[07] = InstantiateChecks("cLocationChecks", "LocationChecks", checksDllPath, true);
                Checks[08] = InstantiateChecks("cProgramChecks", "ProgramChecks", checksDllPath, true);
                Checks[09] = InstantiateChecks("cMethodChecks", "MethodChecks", checksDllPath, true);
                Checks[10] = InstantiateChecks("cComponentChecks", "ComponentChecks", checksDllPath, false);
                Checks[11] = InstantiateChecks("cSystemChecks", "SystemChecks", checksDllPath, false);
                Checks[12] = InstantiateChecks("cFuelFlowChecks", "FuelFlowChecks", checksDllPath, false);
                Checks[13] = InstantiateChecks("cFormulaChecks", "FormulaChecks", checksDllPath, false);
                Checks[14] = InstantiateChecks("cSpanChecks", "SpanChecks", checksDllPath, false);
                Checks[15] = InstantiateChecks("cDefaultChecks", "DefaultChecks", checksDllPath, false);
                Checks[16] = InstantiateChecks("cLoadChecks", "LoadChecks", checksDllPath, false);
                Checks[17] = InstantiateChecks("cQualificationChecks", "QualificationChecks", checksDllPath, false);
                Checks[18] = InstantiateChecks("cFuelChecks", "FuelChecks", checksDllPath, false);
                Checks[19] = InstantiateChecks("cUnitControlChecks", "UnitControlChecks", checksDllPath, false);
                Checks[20] = InstantiateChecks("cCapacityChecks", "UnitCapacityChecks", checksDllPath, false);
                Checks[58] = InstantiateChecks("cQualificationLEEChecks", "QualificationLEEChecks", checksDllPath, false);
                Checks[59] = InstantiateChecks("cMATSSupplementalMethodChecks", "MATSSupplementalMethodChecks", checksDllPath, false);

                result = true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.FormatError();
                result = false;
            }

            return result;
        }

        protected override string ExecuteChecksWork()
        {
            try
            {
                bool RunResult;
                string Result = "";
                Console.WriteLine("*****************************************************Running Monitoring Plan Checks  ");

                // Create category objects with check bands initialized
                cMonitorPlanCategory MonitorPlanCategory = cMonitorPlanCategory.GetInitialized(mCheckEngine, this);
                cMonitorLocation MonitorLocation = cMonitorLocation.GetInitialized(mCheckEngine, this);
                cMonitorLocationAttribute MonitorLocationAttribute = cMonitorLocationAttribute.GetInitialized(mCheckEngine, this);
                cUnitStackConfiguration UnitStackConfiguration = cUnitStackConfiguration.GetInitialized(mCheckEngine, this);
                cUnitProgram UnitProgramCategory = cUnitProgram.GetInitialized(mCheckEngine, this);
                cUnitProgramParameterCategory UnitProgramParameterCategory = cUnitProgramParameterCategory.GetInitialized(UnitProgramCategory);
                cComponent Component = cComponent.GetInitialized(mCheckEngine, this);
                cAnalyzerRange AnalyzerRange = cAnalyzerRange.GetInitialized(mCheckEngine, this);
                //cCalibrationStandardDataCategory CalibrationStandard = cCalibrationStandardDataCategory.GetInitialized(mCheckEngine, this);
                cSystemCategory MonitorSystem = cSystemCategory.GetInitialized(mCheckEngine, this);
                cSystemComponentCategory SystemComponentCategory = cSystemComponentCategory.GetInitialized(mCheckEngine, this);
                cSystemFuelFlowCategory SystemFuelFlowCategory = cSystemFuelFlowCategory.GetInitialized(mCheckEngine, this);
                cFormula Formula = cFormula.GetInitialized(mCheckEngine, this);
                cSpanCategory SpanCategory = cSpanCategory.GetInitialized(mCheckEngine, this);
                cDefaultCategory Default = cDefaultCategory.GetInitialized(mCheckEngine, this);
                cWAFCategory Waf = cWAFCategory.GetInitialized(mCheckEngine, this);
                cLoadCategory Load = cLoadCategory.GetInitialized(mCheckEngine, this);
                cQualificationCategory Qual = cQualificationCategory.GetInitialized(mCheckEngine, this);
                cQualificationPercentCategory QualPct = cQualificationPercentCategory.GetInitialized(mCheckEngine, this);
                cQualificationLMECategory QualLME = cQualificationLMECategory.GetInitialized(mCheckEngine, this);
                cQualificationLEECategory QualLEE = cQualificationLEECategory.GetInitialized(mCheckEngine, this);
                cMethod _Method = cMethod.GetInitialized(mCheckEngine, this);
                cUnitControlCategory _UnitControl = cUnitControlCategory.GetInitialized(mCheckEngine, this);
                cUnitCapacityCategory _UnitCapacity = cUnitCapacityCategory.GetInitialized(mCheckEngine, this);
                cUnitFuelCategory _UnitFuel = cUnitFuelCategory.GetInitialized(mCheckEngine, this);
                cMonitorPlanCommentCategory MonitorPlanCommentCategory = cMonitorPlanCommentCategory.GetInitialized(mCheckEngine, this);
                cMATSSupplementalMethodCategory MATSMethod = cMATSSupplementalMethodCategory.GetInitialized(mCheckEngine, this);

                SetCheckParameter("First_ECMPS_Reporting_Period", CheckEngine.FirstEcmpsReportingPeriodId);

                RunResult = MonitorPlanCategory.ProcessChecks(mCheckEngine.MonPlanId);

                if (RunResult == true && (bool)MonitorPlanCategory.GetCheckParameter("Evaluate_Monitoring_Plan").ParameterValue)
                {
                    //MONLOC
                    foreach (DataRow drMonitorLocation in mSourceData.Tables["MpLocation"].Rows)
                    {
                        string MonitorLocationFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";
                        MonLocId = (string)drMonitorLocation["mon_loc_id"];

                        RunResult = MonitorLocation.ProcessChecks(MonLocId);

                        if (RunResult == true && !(bool)MonitorLocation.GetCheckParameter("Abort_Location_Evaluation").ParameterValue)
                        {
                            //LOCCHAR
                            mSourceData.Tables["LocationAttribute"].DefaultView.RowFilter = "mon_loc_id = '" + (string)drMonitorLocation["mon_loc_id"] + "'";

                            foreach (DataRowView drvMonitorLocationAttribute in mSourceData.Tables["LocationAttribute"].DefaultView)
                            {
                                RunResult = MonitorLocationAttribute.ProcessChecks((string)drvMonitorLocationAttribute["mon_loc_attrib_id"], MonLocId);
                                MonitorLocationAttribute.EraseParameters();
                            }

                            //UNITSTK
                            mSourceData.Tables["UnitStackConfiguration"].DefaultView.RowFilter = "stack_pipe_id = '" + cDBConvert.ToString(drMonitorLocation["stack_pipe_id"]) + "'";

                            foreach (DataRowView drvUnitStackConfiguration in mSourceData.Tables["UnitStackConfiguration"].DefaultView)
                            {
                                RunResult = UnitStackConfiguration.ProcessChecks((string)drvUnitStackConfiguration["config_id"], MonLocId);
                                UnitStackConfiguration.EraseParameters();
                            }

                            //PROGRAM
                            mSourceData.Tables["LocationProgram"].DefaultView.RowFilter = "unit_id = " + cDBConvert.ToLong(drMonitorLocation["unit_id"]) + "";

                            foreach (DataRowView drvUnitProgram in mSourceData.Tables["LocationProgram"].DefaultView)
                            {
                                if (cDBConvert.ToString(drvUnitProgram["LOCATION_IDENTIFIER"]) == cDBConvert.ToString(drvUnitProgram["UNITID"]))
                                {
                                    RunResult = UnitProgramCategory.ProcessChecks(cDBConvert.ToString(drvUnitProgram["up_id"]), MonLocId);

                                    //PRGPRAM
                                    {
                                        if (GetCheckParameter("Current_Program_Active").AsBoolean(false))
                                        {
                                            mSourceData.Tables["UnitProgramParameter"].DefaultView.RowFilter = string.Format("UP_ID = {0}", drvUnitProgram["UP_ID"]);

                                            foreach (DataRowView unitProgramParameterRow in mSourceData.Tables["UnitProgramParameter"].DefaultView)
                                            {
                                                RunResult = UnitProgramParameterCategory.ProcessChecks(MonLocId, unitProgramParameterRow["PRG_PARAM_ID"].AsLong());
                                                UnitProgramParameterCategory.EraseParameters();
                                            }
                                        }
                                    }

                                    UnitProgramCategory.EraseParameters();
                                }
                            }

                            //COMP
                            mSourceData.Tables["Component"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvComponent in mSourceData.Tables["Component"].DefaultView)
                            {
                                RunResult = Component.ProcessChecks((string)drvComponent["component_id"], MonLocId);

                                if (RunResult == true)
                                {
                                    //ANRANGE
                                    mSourceData.Tables["AnalyzerRange"].DefaultView.RowFilter = "component_id = '" + cDBConvert.ToString(drvComponent["component_id"]) + "'";

                                    foreach (DataRowView drvAnalyzerRange in mSourceData.Tables["AnalyzerRange"].DefaultView)
                                    {
                                        RunResult = AnalyzerRange.ProcessChecks((string)drvAnalyzerRange["analyzer_range_id"], MonLocId);
                                        AnalyzerRange.EraseParameters();
                                    }
                                }

                                Component.EraseParameters();
                            }

                            //Monitor System
                            mSourceData.Tables["MonitorSystem"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvSystem in mSourceData.Tables["MonitorSystem"].DefaultView)
                            {
                                RunResult = MonitorSystem.ProcessChecks((string)drvSystem["mon_sys_id"], MonLocId);

                                if (RunResult == true)
                                {
                                    string MonSysId = cDBConvert.ToString(drvSystem["mon_sys_id"]);

                                    //SYSCOMP
                                    mSourceData.Tables["MonitorSystemComponent"].DefaultView.RowFilter = "mon_sys_id = '" + MonSysId + "'";

                                    foreach (DataRowView drvSystemComponentCategory in mSourceData.Tables["MonitorSystemComponent"].DefaultView)
                                    {
                                        string monSysCompId = drvSystemComponentCategory["mon_sys_comp_id"].AsString();
                                        RunResult = SystemComponentCategory.ProcessChecks(monSysCompId, MonLocId);
                                        SystemComponentCategory.EraseParameters();
                                    }

                                    //FUELFLW
                                    mSourceData.Tables["SystemFuelFlow"].DefaultView.RowFilter = "mon_sys_id = '" + MonSysId + "'";

                                    foreach (DataRowView SystemFuelFlowRow in mSourceData.Tables["SystemFuelFlow"].DefaultView)
                                    {
                                        RunResult = SystemFuelFlowCategory.ProcessChecks((string)SystemFuelFlowRow["sys_fuel_id"], MonSysId, MonLocId);
                                        SystemFuelFlowCategory.EraseParameters();
                                    }
                                }

                                MonitorSystem.EraseParameters();
                            }

                            //FORMULA
                            mSourceData.Tables["MonitorFormula"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvFormula in mSourceData.Tables["MonitorFormula"].DefaultView)
                            {
                                RunResult = Formula.ProcessChecks((string)drvFormula["mon_form_id"], MonLocId);
                                Formula.EraseParameters();
                            }

                            //SPAN
                            mSourceData.Tables["MonitorSpan"].DefaultView.RowFilter = MonitorLocationFilter;

                            foreach (DataRowView SpanRow in mSourceData.Tables["MonitorSpan"].DefaultView)
                            {
                                RunResult = SpanCategory.ProcessChecks((string)SpanRow["Span_Id"], MonLocId);
                                SpanCategory.EraseParameters();
                            }

                            //DEFAULT
                            mSourceData.Tables["MonitorDefault"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvDefault in mSourceData.Tables["MonitorDefault"].DefaultView)
                            {
                                RunResult = Default.ProcessChecks((string)drvDefault["mondef_id"], MonLocId);
                                Default.EraseParameters();
                            }

                            //WAF
                            mSourceData.Tables["RectDuctWaf"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvWAF in mSourceData.Tables["RectDuctWaf"].DefaultView)
                            {
                                RunResult = Waf.ProcessChecks((string)drvWAF["rect_duct_waf_data_id"], MonLocId);
                                Waf.EraseParameters();
                            }

                            //LOAD
                            mSourceData.Tables["MonitorLoad"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvLoad in mSourceData.Tables["MonitorLoad"].DefaultView)
                            {
                                RunResult = Load.ProcessChecks((string)drvLoad["load_id"], MonLocId);
                                Load.EraseParameters();
                            }

                            //QUAL
                            mSourceData.Tables["MonitorQualification"].DefaultView.RowFilter = "mon_loc_id = '" + cDBConvert.ToString(drMonitorLocation["mon_loc_id"]) + "'";

                            foreach (DataRowView drvQual in mSourceData.Tables["MonitorQualification"].DefaultView)
                            {
                                RunResult = Qual.ProcessChecks((string)drvQual["mon_qual_id"], MonLocId);

                                if (RunResult == true)
                                {
                                    //QUALPCT
                                    mSourceData.Tables["MonitorQualificationPct"].DefaultView.RowFilter = "mon_qual_id = '" + cDBConvert.ToString(drvQual["mon_qual_id"]) + "'";

                                    foreach (DataRowView drvQualPct in mSourceData.Tables["MonitorQualificationPct"].DefaultView)
                                    {
                                        RunResult = QualPct.ProcessChecks((string)drvQualPct["mon_pct_id"], MonLocId);
                                        QualPct.EraseParameters();
                                    }

                                    //QUALLME
                                    mSourceData.Tables["MonitorQualificationLME"].DefaultView.RowFilter = "mon_qual_id = '" + cDBConvert.ToString(drvQual["mon_qual_id"]) + "'";

                                    foreach (DataRowView drvQualLME in mSourceData.Tables["MonitorQualificationLME"].DefaultView)
                                    {
                                        RunResult = QualLME.ProcessChecks((string)drvQualLME["mon_LME_id"], MonLocId);
                                        QualLME.EraseParameters();
                                    }
                                    //QUALLEE
                                    mSourceData.Tables["MonitorQualificationLEE"].DefaultView.RowFilter = "mon_qual_id = '" + cDBConvert.ToString(drvQual["mon_qual_id"]) + "'";

                                    foreach (DataRowView drvQualLEE in mSourceData.Tables["MonitorQualificationLEE"].DefaultView)
                                    {
                                        RunResult = QualLEE.ProcessChecks((string)drvQualLEE["mon_LEE_id"], MonLocId);
                                        QualLEE.EraseParameters();
                                    }
                                }

                                Qual.EraseParameters();
                            }

                            // Method
                            mSourceData.Tables["MonitorMethod"].DefaultView.RowFilter = string.Format("mon_loc_id = '{0}'", cDBConvert.ToString(drMonitorLocation["mon_loc_id"]));

                            foreach (DataRowView drvMethod in mSourceData.Tables["MonitorMethod"].DefaultView)
                            {
                                RunResult = _Method.ProcessChecks((string)drvMethod["mon_method_id"], MonLocId);
                                _Method.EraseParameters();
                            }

                            // MATS Supplemental Method
                            mSourceData.Tables["MATSMethodData"].DefaultView.RowFilter = string.Format("mon_loc_id = '{0}'", cDBConvert.ToString(drMonitorLocation["mon_loc_id"]));

                            foreach (DataRowView drvMethod in mSourceData.Tables["MATSMethodData"].DefaultView)
                            {
                                RunResult = MATSMethod.ProcessChecks((string)drvMethod["MATS_Method_ID"], MonLocId);
                                MATSMethod.EraseParameters();
                            }

                            // UnitControl
                            string _UnitControlFilter = string.Format("mon_loc_id = '{0}' and Location_Identifier = UnitId", cDBConvert.ToString(drMonitorLocation["mon_loc_id"]));
                            mSourceData.Tables["LocationControl"].DefaultView.RowFilter = _UnitControlFilter;

                            foreach (DataRowView drvUnitControl in mSourceData.Tables["LocationControl"].DefaultView)
                            {
                                RunResult = _UnitControl.ProcessChecks(drvUnitControl["ctl_id"].ToString(), MonLocId);
                                _UnitControl.EraseParameters();
                            }

                            // UnitCapacity
                            string _UnitCapacityFilter = string.Format("mon_loc_id = '{0}' and Location_Identifier = UnitId", cDBConvert.ToString(drMonitorLocation["mon_loc_id"]));
                            mSourceData.Tables["LocationCapacity"].DefaultView.RowFilter = _UnitCapacityFilter;

                            foreach (DataRowView drvUnitCapacity in mSourceData.Tables["LocationCapacity"].DefaultView)
                            {
                                RunResult = _UnitCapacity.ProcessChecks(drvUnitCapacity["unit_cap_id"].ToString(), MonLocId);
                                _UnitCapacity.EraseParameters();
                            }

                            // UnitFuel
                            string _UnitFuelFilter = string.Format("mon_loc_id = '{0}' and Location_Identifier = UnitId", cDBConvert.ToString(drMonitorLocation["mon_loc_id"]));
                            mSourceData.Tables["LocationFuel"].DefaultView.RowFilter = _UnitFuelFilter;

                            foreach (DataRowView drvUnitFuel in mSourceData.Tables["LocationFuel"].DefaultView)
                            {
                                RunResult = _UnitFuel.ProcessChecks(drvUnitFuel["uf_id"].ToString(), MonLocId);
                                _UnitFuel.EraseParameters();
                            }

                        }

                        MonitorLocation.EraseParameters();
                    }

                    //MPCOMM
                    mSourceData.Tables["MpMonitorPlanComment"].DefaultView.RowFilter = "mon_plan_id = '" + mCheckEngine.MonPlanId + "'";

                    foreach (DataRowView MonitorPlanCommentRow in mSourceData.Tables["MpMonitorPlanComment"].DefaultView)
                    {
                        RunResult = MonitorPlanCommentCategory.ProcessChecks((string)MonitorPlanCommentRow["mon_plan_comment_id"]);
                        MonitorPlanCommentCategory.EraseParameters();
                    }
                }

                MonitorPlanCategory.EraseParameters();

                DbUpdate(ref Result);


                return Result;
            }
            catch (Exception e) {

                Console.WriteLine("******************************************************** checks Failed" + e.ToString());
                
            }
            return false;
        }

        protected override void InitCalculatedData()
        {
            return;
        }

        protected override void InitSourceData()
        {
            mSourceData = new DataSet();
            mFacilityID = GetFacilityID();

            string FacilityWhere = string.Format("WHERE FAC_ID = '{0}'", mFacilityID);
            string MonitorPlanWhere = string.Format("WHERE MON_PLAN_ID = '{0}'", mCheckEngine.MonPlanId);

            //Monitor Plan Based Tables
            AddSourceDataTable("MpLocation", "camdecmpswks.vw_mp_location", MonitorPlanWhere); //SQL Server Source: "VW_MP_LOCATION"
            AddSourceDataTable("MpMonitorPlan", "camdecmpswks.vw_mp_monitor_plan", MonitorPlanWhere); //SQL Server Source: "VW_MP_MONITOR_PLAN"
            AddSourceDataTable("MpMonitorPlanComment", "camdecmpswks.vw_monitor_plan_comment", MonitorPlanWhere); //SQL Server Source: "VW_MONITOR_PLAN_COMMENT"
            AddSourceDataTable("MpUnitProgram", "camdecmpswks.vw_mp_unit_program", MonitorPlanWhere); //SQL Server Source: "VW_MP_UNIT_PROGRAM"
            AddSourceDataTable("EmissionsEvaluation", "camdecmpswks.vw_evem_emissions", MonitorPlanWhere); //SQL Server Source: "vw_EVEM_Emissions"


            /* Data Filtered by the Monitor Plan */

            AddSourceDataTable("MPRelatedLocation", "camdecmpswks.vw_mp_related_location", MonitorPlanWhere); // MP Locations and Related Locations. SQL Server Source: "VW_MP_Related_Location"
            AddSourceDataTable("MpMonitorMethod", "camdecmpswks.vw_mp_monitor_method", MonitorPlanWhere); //SQL Server Source: "VW_MP_MONITOR_METHOD"

            // Filter for all location related to the monitoring plan
            string AllRelatedLocationsWhere;
            {
                string MonitorLocationList = "";
                string MonitorLocationDelim = "";

                foreach (DataRow MonitorLocationRow in mSourceData.Tables["MPRelatedLocation"].Rows)
                {
                    MonitorLocationList += MonitorLocationDelim + "'" + cDBConvert.ToString(MonitorLocationRow["Mon_Loc_Id"]) + "'";
                    MonitorLocationDelim = ", ";
                }

                if (MonitorLocationList != "")
                    AllRelatedLocationsWhere = string.Format("WHERE MON_LOC_ID IN ({0})", MonitorLocationList);
                else
                    AllRelatedLocationsWhere = "WHERE MON_LOC_ID = ''";
            }

            // Filter for only the locations in the monitoring plan
            string MonitorPlanLocationsWhere;
            {
                string MonitorLocationList = "";
                string MonitorLocationDelim = "";

                foreach (DataRow MonitorLocationRow in mSourceData.Tables["MpLocation"].Rows)
                {
                    MonitorLocationList += MonitorLocationDelim + "'" + cDBConvert.ToString(MonitorLocationRow["Mon_Loc_Id"]) + "'";
                    MonitorLocationDelim = ", ";
                }

                if (MonitorLocationList != "")
                    MonitorPlanLocationsWhere = string.Format("WHERE MON_LOC_ID IN ({0})", MonitorLocationList);
                else
                    MonitorPlanLocationsWhere = "WHERE MON_LOC_ID = ''";
            }

            //Monitor Plan Data Tables for Locations
           
            AddSourceDataTable("AnalyzerRange", "camdecmpswks.vw_analyzer_range", AllRelatedLocationsWhere); //SQL Server Source: "VW_Analyzer_range"
            AddSourceDataTable("SystemAnalyzerRange", "camdecmpswks.vw_system_analyzer_range", AllRelatedLocationsWhere); //SQL Server Source: "VW_Analyzer_range"
            AddSourceDataTable("Component", "camdecmpswks.vw_component", AllRelatedLocationsWhere);  //SQL Server Source: "VW_COMPONENT"
        //  AddSourceDataTable("CalibrationStandard", "VW_CALIBRATION_STANDARD", AllRelatedLocationsWhere);
            AddSourceDataTable("MonitorDefault", "camdecmpswks.vw_monitor_default", AllRelatedLocationsWhere);  //SQL Server Source: "VW_MONITOR_DEFAULT"
            AddSourceDataTable("MonitorFormula", "camdecmpswks.vw_monitor_formula", AllRelatedLocationsWhere);  //SQL Server Source: "VW_MONITOR_FORMULA"
            AddSourceDataTable("MonitorLoad", "camdecmpswks.vw_monitor_load", AllRelatedLocationsWhere);  //SQL Server Source: "VW_MONITOR_LOAD"
            AddSourceDataTable("MonitorMethod", "camdecmpswks.vw_monitor_method", AllRelatedLocationsWhere);  //SQL Server Source: "VW_MONITOR_METHOD"
            AddSourceDataTable("MATSMethodData", string.Format("camdecmpswks.mats_method_data_parameter('{0}')", mCheckEngine.MonPlanId), AllRelatedLocationsWhere);  //SQL Server Source: "CheckMp.MATSMethodDataParameter('{0}')"
            AddSourceDataTable("MATSCombinedMethod", string.Format("camdecmpswks.mats_combined_method('{0}')", mCheckEngine.MonPlanId), AllRelatedLocationsWhere);  //SQL Server Source: "CheckMp.MatsCombinedMethod('{0}')"
            AddSourceDataTable("CombinedFacilityMethodRecords", string.Format("camdecmpswks.combined_methods('{0}')", int.Parse(mFacilityID.ToString())), AllRelatedLocationsWhere);  //SQL Server Source: "CheckMp.CombinedMethods('{0}')"

            AddSourceDataTable("MonitorQualification", "camdecmpswks.vw_monitor_qualification", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_QUALIFICATION"
            AddSourceDataTable("MonitorQualificationPct", "camdecmpswks.vw_monitor_qualification_pct", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_QUALIFICATION_PCT"
            AddSourceDataTable("MonitorQualificationLME", "camdecmpswks.vw_monitor_qualification_lme", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_QUALIFICATION_LME"
            AddSourceDataTable("MonitorQualificationLEE", string.Format("camdecmpswks.monitor_qualification_lee_parameter('{0}')", mCheckEngine.MonPlanId), AllRelatedLocationsWhere); //SQL Server Source: "CheckMp.MonitorQualificationLEEParameter('{0}')"
            AddSourceDataTable("MonitorSpan", "camdecmpswks.vw_monitor_span", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_SPAN"
            AddSourceDataTable("MonitorSystem", "camdecmpswks.vw_monitor_system", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_SYSTEM"
            AddSourceDataTable("MonitorSystemComponent", "camdecmpswks.vw_monitor_system_component", AllRelatedLocationsWhere); //SQL Server Source: "VW_MONITOR_SYSTEM_COMPONENT"
            AddSourceDataTable("SystemFuelFlow", "camdecmpswks.vw_system_fuel_flow", AllRelatedLocationsWhere); //SQL Server Source: "VW_SYSTEM_FUEL_FLOW"
            AddSourceDataTable("UsedIdentifier", "camdecmpswks.vw_used_identifier", AllRelatedLocationsWhere); //SQL Server Source: "VW_USED_IDENTIFIER"
            AddSourceDataTable("RectDuctWaf", "camdecmpswks.vw_rect_duct_waf", AllRelatedLocationsWhere); //SQL Server Source: "VW_RECT_DUCT_WAF"
            AddSourceDataTable("UnitMonitorSystem", "camdecmpswks.vw_unit_monitor_system", AllRelatedLocationsWhere); //SQL Server Source: "VW_UNIT_MONITOR_SYSTEM"

            //Facility and Unit Data Tables for Locations
            AddSourceDataTable("LocationAttribute", "camdecmpswks.vw_location_attribute", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_ATTRIBUTE"
            AddSourceDataTable("LocationCapacity", "camdecmpswks.vw_location_capacity", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_CAPACITY"
            AddSourceDataTable("LocationControl", "camdecmpswks.vw_location_control", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_CONTROL"
            AddSourceDataTable("LocationFuel", "camdecmpswks.vw_location_fuel", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_FUEL"
            AddSourceDataTable("LocationProgram", "camdecmpswks.vw_location_program", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_PROGRAM"
            AddSourceDataTable("LocationProgramParameter", string.Format("camdecmpswks.location_program_parameter('{0}')", mCheckEngine.MonPlanId), null); //SQL Server Source: "CheckMp.LocationProgramParameter('{0}')"
            AddSourceDataTable("LocationReportingFrequency", "camdecmpswks.vw_location_reporting_frequency", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_REPORTING_FREQUENCY"
            AddSourceDataTable("LocationUnitType", "camdecmpswks.vw_location_unit_type", AllRelatedLocationsWhere); //SQL Server Source: "VW_LOCATION_UNIT_TYPE"
            AddSourceDataTable("UnitOpStatus", "camdecmpswks.vw_unit_op_status", MonitorPlanLocationsWhere); //SQL Server Source: "VW_UNIT_OP_STATUS"
           AddSourceDataTable("UnitProgramExemption", "camdecmpswks.vw_unit_program_exemption", AllRelatedLocationsWhere); //SQL Server Source: "VW_UNIT_PROGRAM_EXEMPTION"
            AddSourceDataTable("UnitProgramParameter", string.Format("camdecmpswks.unit_program_parameter('{0}')", mCheckEngine.MonPlanId), null); //SQL Server Source: "CheckMp.UnitProgramParameter('{0}')"
            AddSourceDataTable("UnitStackConfiguration", "camdecmpswks.vw_unit_stack_configuration", AllRelatedLocationsWhere); //SQL Server Source: "VW_UNIT_STACK_CONFIGURATION"

            //Facility Data Tables for Facility
            AddSourceDataTable("FacilityLocation", "camdecmpswks.vw_monitor_location", FacilityWhere); //SQL Server Source: "VW_MONITOR_LOCATION"
            AddSourceDataTable("FacilityUnitStackConfiguration", "camdecmpswks.vw_unit_stack_configuration", FacilityWhere); //SQL Server Source: "VW_UNIT_STACK_CONFIGURATION"
            AddSourceDataTable("StackPipe", "camdecmpswks.vw_stack_pipe", FacilityWhere); //SQL Server Source: "VW_STACK_PIPE"
            //AddSourceDataTable("FacilityDefault", "VW_MONITOR_DEFAULT", FacilityWhere);

            //QA-Test Based Tables
            AddSourceDataTable("QASuppData", "camdecmpswks.vw_qa_supp_data", AllRelatedLocationsWhere); //SQL Server Source: "VW_QA_SUPP_DATA"
            AddSourceDataTable("QATestSummary", "camdecmpswks.vw_qa_test_summary", AllRelatedLocationsWhere); //SQL Server Source: "VW_QA_TEST_SUMMARY"

        //    //Lookup Code Tables
        //    AddSourceDataTable("AnalyzerRangeCode", "VW_Analyzer_range_code");
        //    AddSourceDataTable("BypassApproachCode", "VW_BYPASS_APPROACH_CODE");
        ////  AddSourceDataTable("CalibrationStdCode", "VW_CALIBRATION_STANDARD_CODE");
        //    AddSourceDataTable("ComponentTypeCode", "VW_component_Type_code");
        //    AddSourceDataTable("ControlCode", "VW_CONTROL_CODE");
        //    AddSourceDataTable("DefaultPurposeCode", "VW_DEFAULT_PURPOSE_CODE");
        //    AddSourceDataTable("DefaultSourceCode", "VW_DEFAULT_SOURCE_CODE");
        //    AddSourceDataTable("DemMethodCode", "DEM_METHOD_CODE");
        //    AddSourceDataTable("FormulaCode", "VW_EQUATION_CODE");
        //    AddSourceDataTable("FuelCode", "VW_FUEL_CODE");
        //    AddSourceDataTable("FuelGroupCode", "FUEL_GROUP_CODE");
        //    AddSourceDataTable("IndicatorCode", "INDICATOR_CODE");
        //    AddSourceDataTable("MaterialCode", "VW_Material_code");
        //    AddSourceDataTable("MatsMethodCode", "Lookup.MATS_METHOD_CODE");
        //    AddSourceDataTable("MatsMethodParameterCode", "Lookup.MATS_METHOD_PARAMETER_CODE");
        //    AddSourceDataTable("MaxRateSourceCode", "VW_Max_Rate_Source_Code");
        //    AddSourceDataTable("MethodCode", "VW_METHOD_CODE");
        //    AddSourceDataTable("ParameterUom", "VW_Parameter_Uom");
        //    AddSourceDataTable("ProgramCode", "Lookup.PROGRAM_CODE", "", "order by PRG_CD");
        //    AddSourceDataTable("QualTypeCode", "VW_QUAL_TYPE_CODE");
        //    AddSourceDataTable("QualLEETestTypeCode", "Lookup.QUAL_LEE_TEST_TYPE_CODE");
        //    AddSourceDataTable("SampleAcquisitionMethodCode", "VW_Sample_Acquisition_Method_Code");
        //    AddSourceDataTable("ShapeCode", "VW_SHAPE_CODE");
        //    AddSourceDataTable("SpanMethodCode", "VW_SPAN_METHOD_CODE");
        //    AddSourceDataTable("SubstituteDataCode", "VW_SUBSTITUTE_DATA_CODE");
        //    AddSourceDataTable("SystemTypeCode", "VW_System_Type_Code");
        //    AddSourceDataTable("SystemDesignationCode", "VW_System_Designation_Code");
        //    AddSourceDataTable("UnitsOfMeasureCode", "VW_Units_Of_Measure_Code");
        //    AddSourceDataTable("WafMethodCode", "VW_WAF_METHOD_CODE");
            AddSourceDataTable("AnalyzerRangeCode", "camdecmpsmd.analyzer_range_code");
            AddSourceDataTable("BypassApproachCode", "camdecmpsmd.bypass_approach_code");
            AddSourceDataTable("ComponentTypeCode", "camdecmpsmd.component_Type_code");
            AddSourceDataTable("ControlCode", "camdecmpsmd.control_code");
            AddSourceDataTable("DefaultPurposeCode", "camdecmpsmd.default_purpose_code");
            AddSourceDataTable("DefaultSourceCode", "camdecmpsmd.default_source_code");
            AddSourceDataTable("DemMethodCode", "camdecmpsmd.dem_method_code");
            AddSourceDataTable("FormulaCode", "camdecmpsmd.equation_code");
            AddSourceDataTable("FuelCode", "camdecmpsmd.fuel_code");
            AddSourceDataTable("FuelGroupCode", "camdecmpsmd.fuel_group_code");
            AddSourceDataTable("IndicatorCode", "camdecmpsmd.fuel_indicator_code");
            AddSourceDataTable("MaterialCode", "camdecmpsmd.material_code");
            AddSourceDataTable("MatsMethodCode", "camdecmpsmd.mats_method_code");
            AddSourceDataTable("MatsMethodParameterCode", "camdecmpsmd.mats_method_parameter_code");
            AddSourceDataTable("MaxRateSourceCode", "camdecmpsmd.max_rate_source_code");
            AddSourceDataTable("MethodCode", "camdecmpsmd.method_code");
            AddSourceDataTable("ParameterUom", "camdecmpsmd.parameter_uom");
            AddSourceDataTable("ProgramCode", "camdmd.program_code", "", "order by PRG_CD");
            AddSourceDataTable("QualTypeCode", "camdecmpsmd.qual_type_code");
            AddSourceDataTable("QualLEETestTypeCode", "camdecmpsmd.qual_lee_test_type_code");
            AddSourceDataTable("SampleAcquisitionMethodCode", "camdecmpsmd.acquisition_method_code");
            AddSourceDataTable("ShapeCode", "camdecmpsmd.shape_code");
            AddSourceDataTable("SpanMethodCode", "camdecmpsmd.span_method_code");
            AddSourceDataTable("SubstituteDataCode", "camdecmpsmd.substitute_data_code");
            AddSourceDataTable("SystemTypeCode", "camdecmpsmd.system_type_code");
            AddSourceDataTable("SystemDesignationCode", "camdecmpsmd.system_designation_code");
            AddSourceDataTable("UnitsOfMeasureCode", "camdecmpsmd.units_of_measure_code");
            AddSourceDataTable("WafMethodCode", "camdecmpsmd.waf_method_code");

            // Miscellaneous Tables
            AddSourceDataTable("SystemParameter", "camdecmpsmd.system_parameter"); //SQL Server Source: "SYSTEM_PARAMETER"

            // Actual Table Crosschecks
            AddSourceDataTable("ParameterAndMethodAndLocationToFormulaCrosscheck", "camdecmpsmd.parameter_method_to_formula"); //SQL Server Source: "CrossCheck.PARAMETER_METHOD_TO_FORMULA"

            // Virtual Table Crosschecks
            LoadCrossChecks();
        }

        /// <summary>
        /// This method initializes the class containing static properties enabling strongly typed access to the parameters used by the process.
        /// </summary>
        protected override void InitStaticParameterClass()
        {
            MpParameters.Init(this);
        }

        /// <summary>
        /// Allows the setting of the current category for which parameters will be set.
        /// </summary>
        /// <param name="category"></param>
        public override void SetStaticParameterCategory(cCategory category)
        {
            MpParameters.Category = category;
        }


        #region Helper Methods

        /// <summary>
        /// Instantiate an object of a particular Emission Checks class.
        /// </summary>
        /// <param name="className">The class to instantiate.</param>
        /// <param name="namespaceLeaf">The namespace of the checks DLLs.</param>
        /// <param name="dllPath">The location of the checks DLLs.</param>
        /// <returns>The resulting checks object.</returns>
        private cChecks InstantiateChecks(string className, string namespaceLeaf, string dllPath, bool isMpChecksChild)
        {
            const string dllName = "MonitorPlan.dll"; //"ECMPS.Checks.MonitorPlan.dll"
            const string nameSpacePath = "ECMPS.Checks";

            object[] constructorArgements = new object[] { this };

            cChecks result;

            if (isMpChecksChild)
                result = (cMpChecks)Activator.CreateInstanceFrom(dllPath + dllName,
                                                                 nameSpacePath + "." + namespaceLeaf + "." + className,
                                                                 true, 0, null,
                                                                 constructorArgements,
                                                                 null, null).Unwrap();
            else
                result = (cChecks)Activator.CreateInstanceFrom(dllPath + dllName,
                                                               nameSpacePath + "." + namespaceLeaf + "." + className).Unwrap();

            return result;
        }

        #endregion

        #endregion


        #region Protected Virtual: DB Update

        /// <summary>
        /// The Update ECMPS Status process identifier.
        /// </summary>
        protected override string DbUpdate_EcmpsStatusProcess { get { return "MP Evaluation"; } }

        /// <summary>
        /// The Update ECMPS Status id key or list for the item(s) for which the update will occur.
        /// </summary>
        protected override string DbUpdate_EcmpsStatusIdKeyOrList { get { return mCheckEngine.MonPlanId; } }

        /// <summary>
        /// The Update ECMPS Status Additional value for the items(s) for which the update will occur..
        /// </summary>
        protected override string DbUpdate_EcmpsStatusOtherField { get { return mCheckEngine.ChkSessionId; } }

        #endregion


        #region Private Methods

        private void LoadCrossChecks()
        {
            DataTable Catalog = mCheckEngine.DbAuxConnection.GetDataTable("SELECT * FROM camdecmpsmd.cross_check_catalog");
            DataTable Value = mCheckEngine.DbAuxConnection.GetDataTable("SELECT * FROM camdecmpswks.vw_cross_check_catalog_value"); //SQL Source: vw_Cross_Check_Catalog_Value
            DataTable CrossCheck;
            DataRow CrossCheckRow;
            string CrossCheckName;
            string Column1Name;
            string Column2Name;
            string Column3Name;

            foreach (DataRow CatalogRow in Catalog.Rows)
            {
                CrossCheckName = (string)CatalogRow["Cross_Chk_Catalog_Name"];
                CrossCheckName = CrossCheckName.Replace(" ", "");

                CrossCheck = new DataTable("CrossCheck_" + CrossCheckName);

                Column1Name = (string)CatalogRow["Description1"];
                Column2Name = (string)CatalogRow["Description2"];

                CrossCheck.Columns.Add(Column1Name);
                CrossCheck.Columns.Add(Column2Name);

                if (CatalogRow["Description3"] != DBNull.Value)
                {
                    Column3Name = (string)CatalogRow["Description3"];
                    CrossCheck.Columns.Add(Column3Name);
                }
                else Column3Name = "";

                Column1Name.Replace(" ", "");
                Column2Name.Replace(" ", "");
                Column3Name.Replace(" ", "");

                Value.DefaultView.RowFilter = "Cross_Chk_Catalog_Id = " + cDBConvert.ToString(CatalogRow["Cross_Chk_Catalog_Id"]);

                foreach (DataRowView ValueRow in Value.DefaultView)
                {
                    CrossCheckRow = CrossCheck.NewRow();

                    CrossCheckRow[Column1Name] = ValueRow["Value1"];
                    CrossCheckRow[Column2Name] = ValueRow["Value2"];

                    if (CatalogRow["Description3"] != DBNull.Value)
                        CrossCheckRow[Column3Name] = ValueRow["Value3"];

                    CrossCheck.Rows.Add(CrossCheckRow);
                }

                mSourceData.Tables.Add(CrossCheck);
            }
        }

        #endregion

    }
}