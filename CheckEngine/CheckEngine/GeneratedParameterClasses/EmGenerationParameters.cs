//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ECMPS.Checks.EmGeneration.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using ECMPS.Checks.CheckEngine;
    using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
    using ECMPS.Checks.TypeUtilities;
    
    
    /// Parameter class for the EmGeneration process
    public sealed class EmGenerationParameters
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
        /// The first day the location reports emissions data during the current reporting period.
        /// </summary>
        public static DateTime? GenBeginDate
        {
            get
            {
                return ((DateTime?)(EmGenerationParameters.GetCheckParameter("Gen_Begin_Date")));
            }
            set
            {
                EmGenerationParameters.SetCheckParameter("Gen_Begin_Date", value);
            }
        }
        
        /// <summary>
        /// The first hour during the first day that the location reports emissions data during the current reporting period.
        /// </summary>
        public static int? GenBeginHour
        {
            get
            {
                return ((int?)(EmGenerationParameters.GetCheckParameter("Gen_Begin_Hour")));
            }
            set
            {
                EmGenerationParameters.SetCheckParameter("Gen_Begin_Hour", value);
            }
        }
        
        /// <summary>
        /// Indicates if the location belongs to an ozone-season program.
        /// </summary>
        public static bool? GenOsReportingRequirement
        {
            get
            {
                return ((bool?)(EmGenerationParameters.GetCheckParameter("Gen_OS_Reporting_Requirement")));
            }
            set
            {
                EmGenerationParameters.SetCheckParameter("Gen_OS_Reporting_Requirement", value);
            }
        }
        
        /// <summary>
        /// The reporting frequency code for the location during the current reporting period.
        /// </summary>
        public static string GenReportingFrequency
        {
            get
            {
                return ((string)(EmGenerationParameters.GetCheckParameter("Gen_Reporting_Frequency")));
            }
            set
            {
                EmGenerationParameters.SetCheckParameter("Gen_Reporting_Frequency", value);
            }
        }
        
        /// <summary>
        /// Program records which apply to the monitoring location.
        ///
        ///For a unit, this is the Unit Program records for the unit.
        ///
        ///For a stack or pipe, this is the Unit Program records for all units linked to the stack or pipe via Unit Stack Configuration records.  The  UnitMonitorCertStartDate for the location program record should be the later of the UnitMonitorCertStartDate in the Unit Program record and the StartDate in the Unit Stack Configuration record.  The  EndDate for the location program record should be the earlier of the EndDate in the Unit Program record and the EndDate in the Unit Stack Configuration record.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwLocationProgramRow> LocationProgramRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("Location_Program_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwLocationProgramRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("Location_Program_Records", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("Location_Program_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Unit Program Reporting Frequency records which apply to the monitoring location.
        ///
        ///For a unit, this is the Unit Reporting Frequency records for the unit.
        ///
        ///For a stack or pipe, this is the Unit Reporting Frequency records for all units linked to the stack or pipe via Unit Stack Configuration records.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwLocationReportingFrequencyRow> LocationReportingFrequencyRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("Location_Reporting_Frequency_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwLocationReportingFrequencyRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("Location_Reporting_Frequency_Records", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("Location_Reporting_Frequency_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Records for Methods for all locations in the monitoring configuration
        ///
        ///This parameter was originally used in Emissions but will also now be used in Monitoring Plan checks.  For Monitoring Plan checks it will include all the methods in the Monitoring Plan being evaluated.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorMethodRow> MpMethodRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("MP_Method_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpMonitorMethodRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("MP_Method_Records", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("MP_Method_Records", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Operating Supp Data Records for the location.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpOpSuppDataRow> OperatingSuppDataRecordsByLocation
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("Operating_Supp_Data_Records_by_Location")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwMpOpSuppDataRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("Operating_Supp_Data_Records_by_Location", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("Operating_Supp_Data_Records_by_Location", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Contains the program code information needed to produce program list for checks.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Lookup.Table.ProgramCodeRow> ProgramCodeTable
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("Program_Code_Table")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Lookup.Table.ProgramCodeRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("Program_Code_Table", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("Program_Code_Table", value.SourceView);
                }
            }
        }
        
        /// <summary>
        /// Contains a list of ozone season program codes.
        /// </summary>
        public static string ProgramIsOzoneSeasonList
        {
            get
            {
                return ((string)(EmGenerationParameters.GetCheckParameter("Program_is_Ozone_Season_List")));
            }
            set
            {
                EmGenerationParameters.SetCheckParameter("Program_is_Ozone_Season_List", value);
            }
        }
        
        /// <summary>
        /// The Unit Stack Configuration records for all of a facility's locations that were retrieved for the evaluation.
        /// </summary>
        public static CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwUnitStackConfigurationRow> UnitStackConfigurationRecords
        {
            get
            {
                System.Data.DataView sourceView = ((System.Data.DataView)(EmGenerationParameters.GetCheckParameter("Unit_Stack_Configuration_Records")));
                if ((sourceView == null))
                {
                    return null;
                }
                else
                {
                    return new CheckDataView<ECMPS.Checks.Data.Ecmps.Dbo.View.VwUnitStackConfigurationRow>(sourceView);
                }
            }
            set
            {
                if ((value == null))
                {
                    EmGenerationParameters.SetCheckParameter("Unit_Stack_Configuration_Records", null);
                }
                else
                {
                    EmGenerationParameters.SetCheckParameter("Unit_Stack_Configuration_Records", value.SourceView);
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
            if (((EmGenerationParameters.Category == null) 
                        == false))
            {
                checkParameter = EmGenerationParameters.Category.GetCheckParameter(parameterId);
            }
            else
            {
                if (((EmGenerationParameters.Process == null) 
                            == false))
                {
                    checkParameter = EmGenerationParameters.Process.GetCheckParameter(parameterId);
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
            if (((EmGenerationParameters.Category == null) 
                        == false))
            {
                EmGenerationParameters.Category.SetCheckParameter(parameterId, parameterValue);
            }
            else
            {
                if (((EmGenerationParameters.Process == null) 
                            == false))
                {
                    EmGenerationParameters.Process.SetCheckParameter(parameterId, parameterValue);
                }
            }
        }
        
        /// <summary>
        /// Initializes the values in the check parameter object.
        /// </summary>
        public static void Init(cProcess process)
        {
            EmGenerationParameters._process = process;
            EmGenerationParameters.Category = null;
            EmGenerationParameters.RegisterParameters();
        }
        
        /// <summary>
        /// Registers each check parameter.
        /// </summary>
        public static void RegisterParameters()
        {
            if (((EmGenerationParameters.Process == null) 
                        || (EmGenerationParameters.Process.ProcessParameters == null)))
            {
                return;
            }
            Process.ProcessParameters.RegisterParameter(3192, "Gen_Begin_Date");
            Process.ProcessParameters.RegisterParameter(3193, "Gen_Begin_Hour");
            Process.ProcessParameters.RegisterParameter(3198, "Gen_OS_Reporting_Requirement");
            Process.ProcessParameters.RegisterParameter(3191, "Gen_Reporting_Frequency");
            Process.ProcessParameters.RegisterParameter(301, "Location_Program_Records");
            Process.ProcessParameters.RegisterParameter(859, "Location_Reporting_Frequency_Records");
            Process.ProcessParameters.RegisterParameter(2847, "MP_Method_Records");
            Process.ProcessParameters.RegisterParameter(2740, "Operating_Supp_Data_Records_by_Location");
            Process.ProcessParameters.RegisterParameter(3593, "Program_Code_Table");
            Process.ProcessParameters.RegisterParameter(3594, "Program_is_Ozone_Season_List");
            Process.ProcessParameters.RegisterParameter(384, "Unit_Stack_Configuration_Records");
        }
    }
}