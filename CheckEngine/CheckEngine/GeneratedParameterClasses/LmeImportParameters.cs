//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ECMPS.Checks.LmeImport.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using ECMPS.Checks.CheckEngine;
    using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
    using ECMPS.Checks.TypeUtilities;
    
    
    /// Parameter class for the LmeImport process
    public sealed class LmeImportParameters
    {
        
        // Storage field for property 'Process'
        private static cProcess _process;
        
        // Storage field for property 'Category'
        private static cCategory _category;
        
        /// The process object for the current evaluation process.  Set by the actual process during initialization.
        public static cProcess Process
        {
            get
            {
                return _process;
            }
        }
        
        /// The current category object set at the beginning of the category's processing of checks and nulled at the end of the processing.
        public static cCategory Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
            }
        }
        
        /// <summary>
        /// The current LME hourly record and associated data in the workspace.
        /// </summary>
        public static ECMPS.Checks.Data.EcmpsWs.Dbo.View.VwEmLmeimportRow CurrentWorkspaceLmeHour
        {
            get
            {
                System.Data.DataRowView sourceRow = ((System.Data.DataRowView)(LmeImportParameters.GetCheckParameter("Current_Workspace_LME_Hour")));
                if ((sourceRow == null))
                {
                    return null;
                }
                else
                {
                    return new ECMPS.Checks.Data.EcmpsWs.Dbo.View.VwEmLmeimportRow(sourceRow);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Current_Workspace_LME_Hour", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Current_Workspace_LME_Hour", value.SourceRow);
                }
            }
        }
        
        /// <summary>
        /// A lookup table of Fuel codes.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.Table.FuelCodeRow> FuelCodeLookupTable
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(LmeImportParameters.GetCheckParameter("Fuel_Code_Lookup_Table")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.Table.FuelCodeRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Fuel_Code_Lookup_Table", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Fuel_Code_Lookup_Table", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Comma delimited list of Location Name, Begin Date and Begin Hour for rows with duplicates in an LME Hourly Op Data import file.
        ///
        ///Use the following format:
        ///
        ///[LocationName]: [BeginDate as YYYY-MM-DD] [BeginHour as HH24]
        /// </summary>
        public static string LmeDuplicateHourlyOpImportList
        {
            get
            {
                return ((string)(LmeImportParameters.GetCheckParameter("LME_Duplicate_Hourly_Op_Import_List")));
            }
            set
            {
                LmeImportParameters.SetCheckParameter("LME_Duplicate_Hourly_Op_Import_List", value);
            }
        }
        
        /// <summary>
        /// The facility ID of the facility in the LME Import file in the workspace.
        /// </summary>
        public static string LmeFacilityId
        {
            get
            {
                return ((string)(LmeImportParameters.GetCheckParameter("LME_Facility_ID")));
            }
            set
            {
                LmeImportParameters.SetCheckParameter("LME_Facility_ID", value);
            }
        }
        
        /// <summary>
        /// The Monitoring Plan ID of the MP in the LME Import file in the workspace.
        /// </summary>
        public static string LmeMpId
        {
            get
            {
                return ((string)(LmeImportParameters.GetCheckParameter("LME_MP_ID")));
            }
            set
            {
                LmeImportParameters.SetCheckParameter("LME_MP_ID", value);
            }
        }
        
        /// <summary>
        /// The ORIS Code of the facility in the LME Import file in the workspace.
        /// </summary>
        public static string LmeOrisCode
        {
            get
            {
                return ((string)(LmeImportParameters.GetCheckParameter("LME_ORIS_Code")));
            }
            set
            {
                LmeImportParameters.SetCheckParameter("LME_ORIS_Code", value);
            }
        }
        
        /// <summary>
        /// The Reporting Period ID of the data in the LME Import file in the workspace.
        /// </summary>
        public static string LmeReportingPeriodId
        {
            get
            {
                return ((string)(LmeImportParameters.GetCheckParameter("LME_Reporting_Period_ID")));
            }
            set
            {
                LmeImportParameters.SetCheckParameter("LME_Reporting_Period_ID", value);
            }
        }
        
        /// <summary>
        /// Records for Methods at Location
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMonitorMethodRow> MethodRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(LmeImportParameters.GetCheckParameter("Method_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMonitorMethodRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Method_Records", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Method_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Records for MonitoringPlanLocation.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorLocationRow> MonitoringPlanLocationRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(LmeImportParameters.GetCheckParameter("Monitoring_Plan_Location_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorLocationRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Monitoring_Plan_Location_Records", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Monitoring_Plan_Location_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Monitor Plan Records
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorPlanRow> MonitorPlanRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(LmeImportParameters.GetCheckParameter("Monitor_Plan_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorPlanRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Monitor_Plan_Records", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Monitor_Plan_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// All LME import records in the workspace.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.EcmpsWs.Dbo.View.VwEmLmeimportRow> WorkspaceLmeRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(LmeImportParameters.GetCheckParameter("Workspace_LME_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.EcmpsWs.Dbo.View.VwEmLmeimportRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    LmeImportParameters.SetCheckParameter("Workspace_LME_Records", null);
                }
                else
                {
                    LmeImportParameters.SetCheckParameter("Workspace_LME_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Gets the parameter value using the category property if it is not null, otherwise using the Process object if it is not null, and otherwise returning null.
        /// </summary>
        /// <param name="parameterId">The string id used to access the parameter in the check parameter collection.</param>
        /// <returns>Returns the requested check parameter as an object if it exists, and as a null if it does not.</returns>
        public static object GetCheckParameter(string parameterId)
        {
            cLegacyCheckParameter checkParameter;
            if (((LmeImportParameters.Category == null) 
                        == false))
            {
                checkParameter = LmeImportParameters.Category.GetCheckParameter(parameterId);
            }
            else
            {
                if (((LmeImportParameters.Process == null) 
                            == false))
                {
                    checkParameter = LmeImportParameters.Process.GetCheckParameter(parameterId);
                }
                else
                {
                    checkParameter = null;
                }
            }
            if ((checkParameter == null))
            {
                return null;
            }
            else
            {
                return checkParameter.ParameterValue;
            }
        }
        
        /// <summary>
        /// Sets the parameter value using the category property if it is not null, otherwise using the Process object if it is not null, and otherwise does nothing.
        /// </summary>
        /// <param name="parameterId">The string id used to access the parameter in the check parameter collection.</param>
        /// <param name="parameterValue">The value to which to set the parameter.</param>
        public static void SetCheckParameter(string parameterId, object parameterValue)
        {
            if (((LmeImportParameters.Category == null) 
                        == false))
            {
                LmeImportParameters.Category.SetCheckParameter(parameterId, parameterValue);
            }
            else
            {
                if (((LmeImportParameters.Process == null) 
                            == false))
                {
                    LmeImportParameters.Process.SetCheckParameter(parameterId, parameterValue);
                }
            }
        }
        
        /// <summary>
        /// Initializes the values in the check parameter object.
        /// </summary>
        public static void Init(cProcess process)
        {
            LmeImportParameters._process = process;
            LmeImportParameters.Category = null;
            LmeImportParameters.RegisterParameters();
        }
        
        /// <summary>
        /// Registers each check parameter.
        /// </summary>
        public static void RegisterParameters()
        {
            if (((LmeImportParameters.Process == null) 
                        || (LmeImportParameters.Process.ProcessParameters == null)))
            {
                return;
            }
            Process.ProcessParameters.RegisterParameter(2768, "Current_Workspace_LME_Hour");
            Process.ProcessParameters.RegisterParameter(430, "Fuel_Code_Lookup_Table");
            Process.ProcessParameters.RegisterParameter(3312, "LME_Duplicate_Hourly_Op_Import_List");
            Process.ProcessParameters.RegisterParameter(2766, "LME_Facility_ID");
            Process.ProcessParameters.RegisterParameter(2767, "LME_MP_ID");
            Process.ProcessParameters.RegisterParameter(2901, "LME_ORIS_Code");
            Process.ProcessParameters.RegisterParameter(2777, "LME_Reporting_Period_ID");
            Process.ProcessParameters.RegisterParameter(340, "Method_Records");
            Process.ProcessParameters.RegisterParameter(1876, "Monitoring_Plan_Location_Records");
            Process.ProcessParameters.RegisterParameter(2903, "Monitor_Plan_Records");
            Process.ProcessParameters.RegisterParameter(2765, "Workspace_LME_Records");
        }
    }
}
