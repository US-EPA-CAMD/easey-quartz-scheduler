using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;
using Npgsql;
using NpgsqlTypes;

namespace ECMPS.Checks.CheckEngine
{
    /// <summary>
    /// Modc Data borders class
    /// </summary>
    public class cModcDataBordersDictionary
    {

        #region Constructors
        /// <summary>
        /// Creates a new cModcDataBordersDictionary object
        /// </summary>
        public cModcDataBordersDictionary()
        {}

        #region  MODC Data Borders Dictionary Methods and Properties

        /// <summary>
        /// Contains a dictionary containing the parameter and moisture-basis key and the cModcDataBorders item.
        /// Values are loaded into the dictionary by hourly check category objects.
        /// </summary>
        public Dictionary<string, cModcDataBorders> ModcDataBordersDictionary { get; set; }

        /// <summary>
        /// Adds the MODC Data Borders object for the Parameter Code, and Moisture Basis when applicable, to the MODC Data Borders Dictionary.
        /// </summary>
        /// <param name="hourlyTypeCd">The Hourly Type Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="parameterCd">The Parameter Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="moistureBasis">The Moisture Basis associated with the MODC Data Borders Dictionary.  Null when a Moisture Basis is not applicable.</param>
        /// <param name="modcDataBorders"></param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when locationPos is less than zero or greater than the position indexes in ModcDataBordersDictionaryList.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when hourlyTypeCd, parameterCd or modcDataBorders parameters are null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when hourlyTypeCd, parameterCd or moistureBasis parameters have empty string or whitespace values, 
        /// or when they already contain a dictionary entry.</exception>
        public void AddModcDataBordersFromCategory(string hourlyTypeCd, string parameterCd, string moistureBasis, cModcDataBorders modcDataBorders)
        {
            // Throw exception if an input value has a value that is not unallowed.
            if (hourlyTypeCd == null) throw new ArgumentNullException("hourlyTypeCd");
            if (parameterCd == null) throw new ArgumentNullException("parameterCd");
            if (modcDataBorders == null) throw new ArgumentNullException("modcDataBorders");
            if (string.IsNullOrWhiteSpace(hourlyTypeCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "hourlyTypeCd");
            if (string.IsNullOrWhiteSpace(parameterCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "parameterCd");
            if (string.IsNullOrWhiteSpace(moistureBasis) && (moistureBasis != null)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "moistureBasis");

            // Initialize the dictionary if it has not been initialized
            if (ModcDataBordersDictionary == null) ModcDataBordersDictionary = new Dictionary<string, cModcDataBorders>();

            // Formation Dictionary Key.
            string dictionaryKey = GetParameterMoistureBasisKey(hourlyTypeCd, parameterCd, moistureBasis);

            // Throw exception if the supplied parameterCd and moistureBasis combination already exists in the dictionary.
            if (ModcDataBordersDictionary.ContainsKey(dictionaryKey)) throw new ArgumentException($"MODC Data Borders Dictionary already contains an entry for {dictionaryKey}.");

            // Add modcDataBorders to the dictionary for hourlyTypeCd, parameterCd and moistureBasis. 
            ModcDataBordersDictionary.Add(dictionaryKey, modcDataBorders);
        }


        /// <summary>
        /// Initializes the MODC Data Borders Dictionary.
        /// </summary>
        public bool InitializeModcDataBordersDictionary()
        {
            ModcDataBordersDictionary = new Dictionary<string, cModcDataBorders>();
            return true;
        }


        /// <summary>
        /// Loads Hour Before Supplemental Data from the previous quarter.
        /// </summary>
        /// <param name="monPlanId">The MON_PLAN_ID for the emission report monitoring plan.</param>
        /// <param name="rptPeriodId">The RPT_PERIOD_ID for the emission report quarter.</param>
        /// <param name="connection">The current "data" connection used by the check engine.</param>
        /// <param name="monitorLocationView">Contains the table with the locations used by the check engine.</param>
        /// <param name="currentYear">The year of the emission report quarter.</param>
        /// <param name="currentQuarter">The quarter of the emission report quarter.</param>
        /// <param name="errorMessage">The error message reference which is updated if an error occurs during the initialization.</param>
        /// <returns>Indicates whether the initialization was successful.</returns>


        public bool InitializeFromPreviousQuarter(string monPlanId, int rptPeriodId, NpgsqlConnection connection, DataView monitorLocationView,
                                                        int currentYear, int currentQuarter, ref string errorMessage)
        //public  bool InitializeFromPreviousQuarter(string monPlanId, int rptPeriodId, SqlConnection connection, DataView monitorLocationView, int currentYear, int currentQuarter, ref string errorMessage)
        {
            
            string missingColumns;

            if (connection == null)
            {
                errorMessage = "Connection does not exists while initializing with Hour Before Supplemental Data from the previous quarter.";
                return false;
            }
            else if (connection.State != ConnectionState.Open)
            {
                errorMessage = "Connection was not open while initializing with Hour Before Supplemental Data from the previous quarter.";
                return false;
            }
            else if (monitorLocationView == null)
            {
                errorMessage = "monitorLocationTable is null and a table is required.";
                return false;
            }
            else if (!IsMonitorLocationTable(monitorLocationView.Table, out missingColumns))
            {
                errorMessage = "monitorLocationTable is not monitor location table.";
                return false;
            }

            // SqlCommand command = connection.CreateCommand();
            try
            {

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

                DataSet previous_quarter_for_system = new DataSet();
                DataSet previous_quarter_non_system = new DataSet();

                NpgsqlCommand command1 = new NpgsqlCommand("SELECT * FROM camdecmpswks.hour_before_supp_data_previous_quarter_for_system(@monplanid, @rptperiodid)", connection);
                command1.Parameters.AddWithValue("monplanid", monPlanId);
                command1.Parameters.AddWithValue("rptperiodid", rptPeriodId);
                adapter.SelectCommand = command1;
                adapter.Fill(previous_quarter_for_system);
                command1.Dispose();

                NpgsqlCommand command2 = new NpgsqlCommand("SELECT * FROM camdecmpswks.hour_before_supp_data_previous_quarter_non_system(@monplanid, @rptperiodid)", connection);
                command2.Parameters.AddWithValue("monplanid", monPlanId);
                command2.Parameters.AddWithValue("rptperiodid", rptPeriodId);
                adapter.SelectCommand = command2;
                adapter.Fill(previous_quarter_non_system);
                command2.Dispose();

                bool result = previous_quarter_for_system.Tables.Count > 0 && previous_quarter_non_system.Tables.Count > 0;

                if (!result)
                {
                    errorMessage = "Stored Procedure did not return any data";
                }
                else
                {
                    DataTable hourBeforeLocationSuppData = previous_quarter_non_system.Tables[0]; 
                    DataTable hourBeforeSystemSuppData = previous_quarter_for_system.Tables[0];

                    if (!IsSuppDataLocationTable(hourBeforeLocationSuppData, out missingColumns))
                    {
                        errorMessage = (missingColumns != null)
                                     ? $"The following columns are missing from the location level HBHA Supplemental Data: {missingColumns}"
                                     : "The location level HBHA Supplemental Data does not contain all of the expected data columns.";

                        result = false;
                    }
                    else if (!IsSuppDataSystemTable(hourBeforeSystemSuppData, out missingColumns))
                    {
                        errorMessage = (missingColumns != null)
                                     ? $"The following columns are missing from the system level HBHA Supplemental Data: {missingColumns}"
                                     : "The system level HBHA Supplemental Data does not contain all of the expected data columns.";

                        result = false;
                    }
                    else if (hourBeforeLocationSuppData.Rows.Count > 0)
                    {
                        for (int locationPos = 0; locationPos < monitorLocationView.Count; locationPos++)
                        {
                            string monLocId = monitorLocationView[locationPos]["MON_LOC_ID"].AsString();

                            DataView hourBeforeLocationSuppDataView = new DataView(hourBeforeLocationSuppData, $"MON_LOC_ID = '{monLocId}'", "MON_LOC_ID", DataViewRowState.CurrentRows);

                            foreach (DataRowView locationSuppDataRow in hourBeforeLocationSuppDataView)
                            {
                                string hourlyTypeCd = locationSuppDataRow["HOURLY_TYPE_CD"].AsString();
                                string parameterCd = locationSuppDataRow["PARAMETER_CD"].AsString();
                                string moistureBasis = locationSuppDataRow["MOISTURE_BASIS"].AsString();

                                try
                                {
                                    InitializeFromPreviousQuarterDo(hourlyTypeCd, parameterCd, moistureBasis, locationPos,
                                                                    locationSuppDataRow["OP_DATEHOUR"].AsBeginDateTime(), 
                                                                    locationSuppDataRow["UNADJUSTED_HRLY_VALUE"].AsDecimal(),
                                                                    locationSuppDataRow["ADJUSTED_HRLY_VALUE"].AsDecimal(),
                                                                    locationSuppDataRow["HRLY_VALUE"].AsDecimal(),
                                                                    currentYear, currentQuarter);

                                    if (hourBeforeSystemSuppData.Rows.Count > 0)
                                    {
                                        string filterFormatString = (moistureBasis == null)
                                                                  ? "MON_LOC_ID = '{0}' and HOURLY_TYPE_CD = '{1}' and PARAMETER_CD = '{2}' and MOISTURE_BASIS is null"
                                                                  : "MON_LOC_ID = '{0}' and HOURLY_TYPE_CD = '{1}' and PARAMETER_CD = '{2}' and MOISTURE_BASIS = '{3}'";

                                        DataView hourBeforeSystemSuppDataView = new DataView(hourBeforeSystemSuppData,
                                                                                             string.Format(filterFormatString, monLocId, hourlyTypeCd, parameterCd, moistureBasis),
                                                                                             "",
                                                                                             DataViewRowState.CurrentRows);

                                        foreach (DataRowView systemSuppDataRow in hourBeforeSystemSuppDataView)
                                        {
                                            InitializeFromPreviousQuarterDo(hourlyTypeCd, parameterCd, moistureBasis, locationPos,
                                                                            systemSuppDataRow["MON_SYS_ID"].AsString(),
                                                                            systemSuppDataRow["OP_DATEHOUR"].AsBeginDateTime(),
                                                                            systemSuppDataRow["UNADJUSTED_HRLY_VALUE"].AsDecimal(),
                                                                            systemSuppDataRow["ADJUSTED_HRLY_VALUE"].AsDecimal(),
                                                                            systemSuppDataRow["HRLY_VALUE"].AsDecimal(),
                                                                            currentYear, currentQuarter);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorMessage = ex.Message;
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (NpgsqlException sqlEx)
            //  catch (SqlException sqlEx)
            {
                //  errorMessage = string.Format("Procedure: {0} - {1}", sqlEx.Procedure, sqlEx.Message);
                errorMessage = string.Format("Procedure: {0} - {1}", sqlEx.InnerException.Message, sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                //command.Dispose();
                //command = null;
            }
            return true;

        }


        #region Helper Methods

        private string[] MonitorLocationColumns = { "LOCATION_NAME", "MON_LOC_ID" };

        private string[] SuppDataLocationColumns = { "ADJUSTED_HRLY_VALUE", "HOURLY_TYPE_CD", "HRLY_VALUE", "LAST_QA_VALUE_SUPP_DATA_ID",
                                                            "MOISTURE_BASIS", "MON_LOC_ID", "MON_PLAN_ID", "OP_DATEHOUR", "PARAMETER_CD",
                                                            "PRIMARY_BYPASS_IND", "RPT_PERIOD_ID", "UNADJUSTED_HRLY_VALUE" };

        private string[] SuppDatSystemColumns = { "ADJUSTED_HRLY_VALUE", "HOURLY_TYPE_CD", "HRLY_VALUE", "LAST_QA_VALUE_SUPP_DATA_ID",
                                                         "MOISTURE_BASIS", "MON_LOC_ID", "MON_PLAN_ID", "MON_SYS_ID", "OP_DATEHOUR",
                                                         "PARAMETER_CD", "RPT_PERIOD_ID", "UNADJUSTED_HRLY_VALUE" };


        /// <summary>
        /// Determines whether a column collection contains a list of columns.
        /// </summary>
        /// <param name="columnCollection">The columns to check.</param>
        /// <param name="columnNames">The list of columns the table must contain.  The table can contain additional columns.</param>
        /// <param name="missingColumns">Contains a comma delimited list of columns that are not contained in the column collection.</param>
        /// <returns> Returns true if the column collection is not null and the columns in the array are all columns in the column collection.  Otherwise returns false.</returns>
        private bool ContainsAllColumns(DataColumnCollection columnCollection, string[] columnNames, out string missingColumns)
        {
            bool result;

            if (columnCollection != null)
            {
                result = true;

                missingColumns = "";

                foreach (string columnName in columnNames)
                {
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        if ((columnCollection == null) || !columnCollection.Contains(columnName))
                        {
                            missingColumns.ListAdd(columnName);
                            result = false;
                        }
                    }
                }

                missingColumns = result ? null : ReadFormatCommaDelimitedList(missingColumns);
            }
            else
            {
                missingColumns = "All";
                result = false;
            }

            return result;
        }


        /// <summary>
        /// Determines whether the column collection for a data row view contains a list of columns.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnNames">The list of columns the table must contain.  The table can contain additional columns.</param>
        /// <param name="missingColumns">Contains a comma delimited list of columns that are not contained in the table.</param>
        /// <returns> Returns true if the column collection is not null and the columns in the array are all columns in the table.  Otherwise returns false.</returns>
        private bool ContainsAllColumns(DataRowView row, string[] columnNames, out string missingColumns)
        {
            bool result;

            if ((row != null) &&
                (row.Row != null) &&
                (row.Row.Table != null))
            {
                result = ContainsAllColumns(row.Row.Table.Columns, columnNames, out missingColumns);
            }
            else
            {
                missingColumns = "All";
                result = false;
            }

            return result;
        }


        /// <summary>
        /// Returns a normalized hourly type, parameter and mositure basis key based on the provided values.
        /// </summary>
        /// <param name="hourlyTypeCd">The hourly type part of the key.</param>
        /// <param name="parameterCd">The parameter part of the key.</param>
        /// <param name="moistureBasis">The moisture basis of the key.</param>
        /// <returns>Returns just the hourly type and parameter if moisture basis is null or whitespace, otherwise returns hourly type, parameter and moisture basis separated by a hypen.  
        /// The hourly type, parameter and moisture basis are trimmed and uppercased.</returns>
        private string GetParameterMoistureBasisKey(string hourlyTypeCd, string parameterCd, string moistureBasis)
        {
            string result;

            if (string.IsNullOrWhiteSpace(moistureBasis))
                result = $"{hourlyTypeCd.Trim().ToUpper()}-{parameterCd.Trim().ToUpper()}";
            else
                result = $"{hourlyTypeCd.Trim().ToUpper()}-{parameterCd.Trim().ToUpper()}-{moistureBasis.Trim().ToUpper()}";

            return result;
        }


        /// <summary>
        /// Adds the last QA operating values to the location level MODC Data Borders Dictionary for the given location position.
        /// Skips the add if the dictionary does not exist.
        /// </summary>
        /// <param name="hourlyTypeCd">The Hourly Type Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="parameterCd">The Parameter Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="moistureBasis">The Moisture Basis associated with the MODC Data Borders Dictionary.  Null when a Moisture Basis is not applicable.</param>
        /// <param name="locationPos">The position emissions checks position for the affected locaiton.</param>
        /// <param name="lastOpDateHour">The last QA operating hour from supplemental data.</param>
        /// <param name="lastUnadjustedValue">The last QA unadjusted value from supplemental data.</param>
        /// <param name="lastAdjustedValue">The last QA adjusted value from supplemental data.</param>
        /// <param name="lastValue">The last QA adjusted/unadjusted value from supplemental data.</param>
        /// <param name="currentYear">The year of the emission report quarter.</param>
        /// <param name="currentQuarter">The quarter of the emission report quarter.</param>
        private void InitializeFromPreviousQuarterDo(string hourlyTypeCd, string parameterCd, string moistureBasis, int locationPos,
                                                           DateTime lastOpDateHour, decimal? lastUnadjustedValue, decimal? lastAdjustedValue, decimal? lastValue, 
                                                           int currentYear, int currentQuarter)
        {
            // Throw exception if an input value has a value that is not unallowed.
            if (locationPos < 0) throw new ArgumentOutOfRangeException("locationPos", $"The value of {locationPos} is less than zero, the minimum value.");
            if (hourlyTypeCd == null) throw new ArgumentNullException("hourlyTypeCd");
            if (parameterCd == null) throw new ArgumentNullException("parameterCd");
            if (string.IsNullOrWhiteSpace(hourlyTypeCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "hourlyTypeCd");
            if (string.IsNullOrWhiteSpace(parameterCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "parameterCd");
            if (string.IsNullOrWhiteSpace(moistureBasis) && (moistureBasis != null)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "moistureBasis");

            // Formation Dictionary Key.
            string dictionaryKey = GetParameterMoistureBasisKey(hourlyTypeCd, parameterCd, moistureBasis);

            // Update the dictionary Item if it exists.
            if ((ModcDataBordersDictionary != null) && ModcDataBordersDictionary.ContainsKey(dictionaryKey) 
                                                    && (ModcDataBordersDictionary[dictionaryKey] != null)
                                                    && (ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocations.Length > locationPos)
                                                    && (ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocations[locationPos] != null))
            {
                // Updated the MODC Data Border Item
                ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocations[locationPos].HandleSuppDataHour(lastOpDateHour, lastUnadjustedValue, lastAdjustedValue, lastValue,
                                                                                                                 currentYear, currentQuarter);
            }
        }


        /// <summary>
        /// Adds the last QA operating values to the location level MODC Data Borders Dictionary for the given location position.
        /// Skips the add if the dictionary does not exist.
        /// </summary>
        /// <param name="hourlyTypeCd">The Hourly Type Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="parameterCd">The Parameter Code associated with the MODC Data Borders Dictionary.</param>
        /// <param name="moistureBasis">The Moisture Basis associated with the MODC Data Borders Dictionary.  Null when a Moisture Basis is not applicable.</param>
        /// <param name="monSysId">The MON_SYS_ID of the system for which supplemental data is being added..</param>
        /// <param name="locationPos">The position emissions checks position for the affected locaiton.</param>
        /// <param name="lastOpDateHour">The last QA operating hour from supplemental data.</param>
        /// <param name="lastUnadjustedValue">The last QA unadjusted value from supplemental data.</param>
        /// <param name="lastAdjustedValue">The last QA adjusted value from supplemental data.</param>
        /// <param name="lastValue">The last QA adjusted/unadjusted value from supplemental data.</param>
        /// <param name="currentYear">The year of the emission report quarter.</param>
        /// <param name="currentQuarter">The quarter of the emission report quarter.</param>
        private void InitializeFromPreviousQuarterDo(string hourlyTypeCd, string parameterCd, string moistureBasis, int locationPos, string monSysId,
                                                           DateTime lastOpDateHour, decimal? lastUnadjustedValue, decimal? lastAdjustedValue, decimal? lastValue, 
                                                           int currentYear, int currentQuarter)
        {
            // Throw exception if an input value has a value that is not unallowed.
            if (locationPos < 0) throw new ArgumentOutOfRangeException("locationPos", $"The value of {locationPos} is less than zero, the minimum value.");
            if (hourlyTypeCd == null) throw new ArgumentNullException("hourlyTypeCd");
            if (parameterCd == null) throw new ArgumentNullException("parameterCd");
            if (string.IsNullOrWhiteSpace(hourlyTypeCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "hourlyTypeCd");
            if (string.IsNullOrWhiteSpace(parameterCd)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "parameterCd");
            if (string.IsNullOrWhiteSpace(moistureBasis) && (moistureBasis != null)) throw new ArgumentException("Argument empty string and whitespace values are not allowed.", "moistureBasis");

            // Formation Dictionary Key.
            string dictionaryKey = GetParameterMoistureBasisKey(hourlyTypeCd, parameterCd, moistureBasis);

            // Update the dictionary Item if it exists.
            if ((ModcDataBordersDictionary != null) && ModcDataBordersDictionary.ContainsKey(dictionaryKey) 
                                                    && (ModcDataBordersDictionary[dictionaryKey] != null)
                                                    && (ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocationSystems.Length > locationPos)
                                                    && (ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocationSystems[locationPos] != null)
                                                    && ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocationSystems[locationPos].ContainsKey(monSysId)
                                                    && (ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocationSystems[locationPos][monSysId] != null))
            {
                // Updated the MODC Data Border Item
                ModcDataBordersDictionary[dictionaryKey].ModcDataBorderLocationSystems[locationPos][monSysId].HandleSuppDataHour(lastOpDateHour, lastUnadjustedValue, lastAdjustedValue, lastValue, 
                                                                                                                                 currentYear, currentQuarter);
            }
        }


        /// <summary>
        /// Returns true if the table is a Location Supplemental Data row.
        /// </summary>
        /// <param name="monitorLocationTable">The row to test.</param>
        /// <param name="missingColumns">Contains a comma delimited list of columns that are not contained in the table.</param>
        /// <returns>Returns true if the row is Daily Interference Check row.</returns>
        private bool IsMonitorLocationTable(DataTable monitorLocationTable, out string missingColumns)
        {
            bool result;

            result = ContainsAllColumns(monitorLocationTable.Columns, MonitorLocationColumns, out missingColumns);

            return result;
        }


        /// <summary>
        /// Returns true if the table is a Location Supplemental Data row.
        /// </summary>
        /// <param name="suppDataLocationTable">The row to test.</param>
        /// <param name="missingColumns">Contains a comma delimited list of columns that are not contained in the table.</param>
        /// <returns>Returns true if the row is Daily Interference Check row.</returns>
        private bool IsSuppDataLocationTable(DataTable suppDataLocationTable, out string missingColumns)
        {
            bool result;

            result = ContainsAllColumns(suppDataLocationTable.Columns, SuppDataLocationColumns, out missingColumns);

            return result;
        }


        /// <summary>
        /// Returns true if the table is a System Supplemental Data row.
        /// </summary>
        /// <param name="suppDataSystemTable">The row to test.</param>
        /// <param name="missingColumns">Contains a comma delimited list of columns that are not contained in the table.</param>
        /// <returns>Returns true if the row is Daily Interference Check row.</returns>
        private bool IsSuppDataSystemTable(DataTable suppDataSystemTable, out string missingColumns)
        {
            bool result;

            result = ContainsAllColumns(suppDataSystemTable.Columns, SuppDatSystemColumns, out missingColumns);

            return result;
        }


        /// <summary>
        /// Formats a common delimited list, stripping trailing commas and replacing the last comma with an 'and'.
        /// </summary>
        /// <param name="list">The list to format.</param>
        /// <returns>The formatted list.</returns>
        private string ReadFormatCommaDelimitedList(string list)
        {
            string result;

            if (string.IsNullOrWhiteSpace(list))
            {
                result = null;
            }
            else
            {
                result = list.Trim();

                if (result[result.Length - 1] == ',')
                    result = result.Substring(0, list.Length - 1);

                if (result.Contains(","))
                {
                    // Replace last comma with an 'and'.
                    int lastCommaPos = result.LastIndexOf(',');
                    if (lastCommaPos != -1) result = result.Remove(lastCommaPos, 1).Insert(lastCommaPos, " and");
                }
            }

            return result;
        }

        #endregion

        #endregion

        #endregion
    }
}
