using System;
using System.Collections.Generic;
using System.Data;

using ECMPS.Checks.DatabaseAccess;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;
using ECMPS.DM.Definitions;
using ECMPS.DM.HourlyEmissions;
using ECMPS.DM.Miscellaneous;
using ECMPS.DM.Utilities;

using Microsoft.Extensions.Logging;

using Npgsql;


namespace ECMPS.Checks.EmissionsReport
{
    /// <summary>
    /// Contains method used to connect to the database.
    /// </summary>
    public class cUpdateEmissionsDb
    {

        #region Public Constructors

        /// <summary>
        /// Creates a DB object with methods to handle DM.cUpdateEmissions calls.
        /// </summary>
        /// <param name="db">The ECMPS EASEY database.</param>
        /// <param name="logger">The ILogger instance to use.</param>
        public cUpdateEmissionsDb(cDatabase db, ILogger logger)
        {
            Db = db;

            _logger = logger;

            ReportFailureCallback = null;
            DisplayLoggedErrorCallback = null;

            CreateErrorLogTable = null;
        }

        #endregion


        #region Public Delegate

        /// <summary>
        /// Error callback delegate.
        /// </summary>
        /// <param name="errorMessage"></param>
        public delegate void dErrorCallback(string errorMessage);

        #endregion


        #region Public Types

        /// <summary>
        /// Stores the information for a check catalog result.
        /// </summary>
        public class cCheckCatalogResult
        {

            /// <summary>
            /// Creates a check catalog result object.
            /// </summary>
            /// <param name="checkCatalogResultId">The check catalog result id of the result.</param>
            /// <param name="checkResult">The check result of the result.</param>
            /// <param name="severityCd">The severity code of the result.</param>
            public cCheckCatalogResult(int? checkCatalogResultId, string checkResult, string severityCd)
            {
                CheckCatalogResultId = checkCatalogResultId;
                CheckResult = checkResult;
                SeverityCd = severityCd;
            }

            /// <summary>
            /// The severity code of the result.
            /// </summary>
            public int? CheckCatalogResultId { get; private set; }

            /// <summary>
            /// The check result value of the result.
            /// </summary>
            public string CheckResult { get; private set; }

            /// <summary>
            /// The severity code of the result.
            /// </summary>
            public string SeverityCd { get; private set; }

        }

        #endregion


        #region Public Properties

        /// <summary>
        /// The table into which errors are logged.
        /// </summary>
        public DataTable CreateErrorLogTable { get; private set; }

        /// <summary>
        /// The ECMPS Aux schema cDatabase object.
        /// </summary>
        public cDatabase Db { get; private set; }

        /// <summary>
        /// Error display callback for testing form.
        /// </summary>
        public dErrorCallback DisplayLoggedErrorCallback { get; private set; }

        /// <summary>
        /// Used to report when processing failed to complete normally.
        /// </summary>
        public dErrorCallback ReportFailureCallback { get; private set; }

        /// <summary>
        /// The active transactions for the database connection.
        /// </summary>
        public NpgsqlTransaction Transaction { get; private set; }

        #endregion


        #region Private Fields

        private readonly ILogger _logger;


        #endregion


        #region Public Methods

        /// <summary>
        /// Loads the Monitor Data Mart Emissions tables, and returns the data needed to
        /// load the Unit Hour table.
        /// </summary>
        /// <param name="monPlanId">MON_PLAN_ID of the emission report.</param>
        /// <param name="rptPeriodId">RPT_PERIOD_ID of the emission report.</param>
        /// <param name="unitInfo"></param>
        /// <param name="locationInfo"></param>
        /// <param name="factorFormulaeArray"></param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the SP executed without error.</returns>
        public bool GetFactorFormulaeArray(string monPlanId, int rptPeriodId,
                                           cUnitInfo[] unitInfo, cLocationInfo[] locationInfo,
                                           out cFactorFormulae[] factorFormulaeArray,
                                           out string errorMessage)
        {
            bool result;

            try
            {
                DataTable apportionmentTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_TABLE( '{monPlanId}', {rptPeriodId} )");

                if (apportionmentTable.Rows.Count == 1)
                {
                    DataTable apportionmentRangeTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_RANGE_TABLE( '{monPlanId}', {rptPeriodId} )");
                    DataTable apportionmentDataTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_DATA_TABLE( '{monPlanId}', {rptPeriodId} )");
                    DataTable apportionmentFormulaeTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_FORMULA_TABLE( '{monPlanId}', {rptPeriodId} )");
                    DataTable apportionmentConditionTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_CONDITION_TABLE( '{monPlanId}', {rptPeriodId} )");
                    DataTable apportionmentSubtractiveTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_APPORTIONMENT_GET_SUBTRACTIVE_TABLE( '{monPlanId}', {rptPeriodId} )");

                    DataView apportionmentRangeView = apportionmentRangeTable.DefaultView;
                    {
                        apportionmentRangeView.Sort = "Begin_DateHour, End_DateHour";
                    }

                    factorFormulaeArray = new cFactorFormulae[apportionmentDataTable.Rows.Count];
                    int factorFormulaeArrayDex = 0;
                    {
                        for (int apportionmentRangeDex = 0; apportionmentRangeDex < apportionmentRangeView.Count; apportionmentRangeDex++)
                        {
                            DataRowView apportionmentRangeRow = apportionmentRangeView[apportionmentRangeDex];
                            int? apportRangeId = apportionmentRangeRow["Apport_Range_Id"].AsInteger();
                            DateTime beginDateHour = apportionmentRangeRow["Begin_DateHour"].AsDateTime(DateTime.MinValue);
                            DateTime endDateHour = apportionmentRangeRow["End_DateHour"].AsDateTime(DateTime.MaxValue);


                            DataView apportionmentDataView = new DataView(apportionmentDataTable,
                                                                            string.Format("Apport_Range_Id = {0}", apportRangeId),
                                                                            "Evaluation_Order",
                                                                            DataViewRowState.CurrentRows);


                            foreach (DataRowView apportionmentDataRow in apportionmentDataView)
                            {
                                int? apportDataId = apportionmentDataRow["Apport_Data_Id"].AsInteger();

                                // Init Factor Formula Object
                                cFactorFormulae factorFormulae = new cFactorFormulae(unitInfo, locationInfo,
                                                                                        beginDateHour.Date,
                                                                                        beginDateHour.Hour,
                                                                                        endDateHour.Date,
                                                                                        endDateHour.Hour);

                                // Set Operating Conditions
                                {
                                    DataView apportionmentConditionView = new DataView(apportionmentConditionTable,
                                                                                        string.Format("Apport_Data_Id = {0}", apportDataId),
                                                                                        "Target_Tag",
                                                                                        DataViewRowState.CurrentRows);

                                    if (apportionmentConditionView.Count > 0)
                                    {
                                        foreach (DataRowView apportionmentConditionRow in apportionmentConditionView)
                                        {
                                            factorFormulae.UpdateOperatingCondition(apportionmentConditionRow["Target_Tag"].AsString(), apportionmentConditionRow["Operating_Ind"].AsBoolean());
                                        }
                                    }
                                }


                                // Add Formulae
                                {
                                    DataView apportionmentFormulaView = new DataView(apportionmentFormulaeTable,
                                                                                        string.Format("Apport_Data_Id = {0}", apportDataId),
                                                                                        "Monitor_Tag, Unit_Tag",
                                                                                        DataViewRowState.CurrentRows);

                                    {
                                        foreach (DataRowView apportionmnetFormulaRow in apportionmentFormulaView)
                                        {
                                            eComplexParameter? complexParameter;
                                            {
                                                switch (apportionmnetFormulaRow["Pollutant_Cd"].AsString())
                                                {
                                                    case "CO2M": complexParameter = eComplexParameter.Co2m; break;
                                                    case "SO2M": complexParameter = eComplexParameter.So2m; break;
                                                    case "NOXM": complexParameter = eComplexParameter.Noxm; break;
                                                    case "HGM": complexParameter = eComplexParameter.Hgm; break;
                                                    case "HCLM": complexParameter = eComplexParameter.Hclm; break;
                                                    case "HFM": complexParameter = eComplexParameter.Hfm; break;
                                                    default: complexParameter = null; break;
                                                }
                                            }

                                            if (complexParameter.HasValue)
                                                factorFormulae.SetFormula(complexParameter.Value,
                                                                            apportionmnetFormulaRow["Unit_Tag"].AsString(),
                                                                            apportionmnetFormulaRow["Monitor_Tag"].AsString(),
                                                                            apportionmnetFormulaRow["Hi_Load_Formula"].AsString(),
                                                                            apportionmnetFormulaRow["Op_Time_Formula"].AsString());
                                            else
                                                factorFormulae.SetFormula(apportionmnetFormulaRow["Unit_Tag"].AsString(),
                                                                            apportionmnetFormulaRow["Monitor_Tag"].AsString(),
                                                                            apportionmnetFormulaRow["Hi_Load_Formula"].AsString(),
                                                                            apportionmnetFormulaRow["Op_Time_Formula"].AsString());
                                        }
                                    }
                                }

                                // Add Reduce By 
                                {
                                    DataView apportionmentSubtractiveView = new DataView(apportionmentSubtractiveTable,
                                                                                            string.Format("Apport_Data_Id = {0}", apportDataId),
                                                                                            "Target_Tag, Subtracting_Tag, Pollutant_Cd",
                                                                                            DataViewRowState.CurrentRows);

                                    {
                                        Dictionary<string, Dictionary<string, List<String>>> subtractiveDict = new Dictionary<string, Dictionary<string, List<String>>>(); // Subtracted Tag, Pollutant, Subtracting Tag

                                        foreach (DataRowView apportionmnetSubtractiveRow in apportionmentSubtractiveView)
                                        {
                                            string targetTag = apportionmnetSubtractiveRow["Target_Tag"].AsString();
                                            string subtractingTag = apportionmnetSubtractiveRow["Subtracting_Tag"].AsString();

                                            string pollutantCd;
                                            {
                                                if (apportionmnetSubtractiveRow["Pollutant_Cd"] != DBNull.Value)
                                                    pollutantCd = apportionmnetSubtractiveRow["Pollutant_Cd"].AsString();
                                                else
                                                    pollutantCd = "ALL";
                                            }

                                            if (!subtractiveDict.ContainsKey(targetTag))
                                            {
                                                subtractiveDict.Add(targetTag, new Dictionary<string, List<String>>());
                                            }

                                            if (!subtractiveDict[targetTag].ContainsKey(pollutantCd))
                                            {
                                                subtractiveDict[targetTag].Add(pollutantCd, new List<String>());
                                            }

                                            if (!subtractiveDict[targetTag][pollutantCd].Contains(subtractingTag))
                                            {
                                                subtractiveDict[targetTag][pollutantCd].Add(subtractingTag);
                                            }
                                        }

                                        foreach (string targetTag in subtractiveDict.Keys)
                                            foreach (string pollutantCd in subtractiveDict[targetTag].Keys)
                                            {
                                                List<String> reduceByTagList = subtractiveDict[targetTag][pollutantCd];
                                                string[] reduceByTags = new string[reduceByTagList.Count];

                                                for (int reduceByDex = 0; reduceByDex < reduceByTagList.Count; reduceByDex++)
                                                    reduceByTags[reduceByDex] = reduceByTagList[reduceByDex];

                                                switch (pollutantCd)
                                                {
                                                    case "ALL": factorFormulae.AddReduceBy(targetTag, reduceByTags); break;
                                                    case "CO2M": factorFormulae.AddReduceBy(eComplexParameter.Co2m, targetTag, reduceByTags); break;
                                                    case "HCLM": factorFormulae.AddReduceBy(eComplexParameter.Hclm, targetTag, reduceByTags); break;
                                                    case "HFM": factorFormulae.AddReduceBy(eComplexParameter.Hfm, targetTag, reduceByTags); break;
                                                    case "HGM": factorFormulae.AddReduceBy(eComplexParameter.Hgm, targetTag, reduceByTags); break;
                                                    case "NOXM": factorFormulae.AddReduceBy(eComplexParameter.Noxm, targetTag, reduceByTags); break;
                                                    case "SO2M": factorFormulae.AddReduceBy(eComplexParameter.So2m, targetTag, reduceByTags); break;
                                                }
                                            }
                                    }
                                }

                                factorFormulaeArray[factorFormulaeArrayDex] = factorFormulae;
                                factorFormulaeArrayDex++;
                            }
                        }
                    }

                    errorMessage = null;
                    result = true;
                }
                else if (apportionmentTable.Rows.Count == 0)
                {
                    factorFormulaeArray = null;
                    errorMessage = null;
                    result = true;
                }
                else
                {
                    throw new Exception("Multiple complex apportionment rows returned");
                }
            }
            catch (Exception ex)
            {
                factorFormulaeArray = null;
                _logger?.LogError(ex, "PDEM.GetFactorFormulaeArray({monPlanId}, {rptPeriodId}, {unitInfo}, {locationInfo})", monPlanId, rptPeriodId, unitInfo, locationInfo);
                errorMessage = ex.Message;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Begins a transaction for the existing connection.
        /// </summary>
        /// <returns></returns>
        public NpgsqlTransaction TransactionBegin()
        {
            Transaction = (Db != null) ? Db.BeginTransaction() : null;

            return Transaction;
        }

        /// <summary>
        /// Commits the current transaction if it exists.
        /// </summary>
        public void TransactionCommit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
            }
        }

        /// <summary>
        /// Rollsback the current transaction if it exists.
        /// </summary>
        public void TransactionRollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
            }
        }

        /// <summary>
        /// Update the check log, set apportionment type, and set emissions created to 'N'.
        /// </summary>
        /// <param name="pdemReportId">The PDEM_REPORT_ID of the update.</param>
        /// <param name="apportionmentType">The apportionment type of the update.</param>
        /// <param name="failureMessage">The failure message to save.</param>
        public void UpdateFailure(long pdemReportId,
                                  eApportionmentType? apportionmentType,
                                  string failureMessage)
        {
            string errorMessage = null;
            bool result;

            try
            {
                Db.CreateTextCommand("call camdecmpsaux.PDEM_UPDATE_FAILURE( $1, $2, $3, $4, $5 )");

                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Bigint).Value = pdemReportId.DbValue();
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Varchar).Value = ((apportionmentType != null) ? apportionmentType?.DbCode() : DBNull.Value);
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Text).Value = failureMessage.DbValue();
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Boolean).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Varchar).Value = DBNull.Value;

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(Db.Command);
                DataTable dataTable = new DataTable("ResultsTable");
                adapter.Fill(dataTable);

                result = (dataTable.Rows.Count > 0) ? bool.Parse(dataTable.Rows[0][0].AsString()) : false;

                if (!result)
                {
                    errorMessage = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][1].AsString() : $"Result row not returned by '{Db.Command.CommandText}'";
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDEM.UpdateFailure({pdemReportId}, {apportionmentType}, {failureMessage})", pdemReportId, apportionmentType, failureMessage);
            }
        }

        /// <summary>
        /// Loads the Monitor Data Mart Emissions tables, and returns the data needed to
        /// load the Unit Hour table.
        /// </summary>
        /// <param name="pdemReportId">The primary key of the PDEM_REPORT row to process.</param>
        /// <param name="monPlanId">The Monitor Plan primary key of the PDEM_REPORT row to process.</param>
        /// <param name="rptPeriodId">The Reporting Period primary key of the PDEM_REPORT row to process.</param>
        /// <param name="isMatsEmissionReport">Indicates whether the emission report is a MATS report.</param>
        /// <param name="locationTable">Table of location information.</param>
        /// <param name="rptPeriodInfoTable">Table of report period information.</param>
        /// <param name="locationTypeCountTable">Table with location type count row.</param>
        /// <param name="locationLinkSpanCountTable">Table with location link type span count row.</param>
        /// <param name="locationLinkActiveTable">Table with location link type active count row.</param>
        /// <param name="specialMethodCountTable">Table with special method type count row.</param>
        /// <param name="monitorHourTable">Montitor hour data.</param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the SP executed without error.</returns>
        public bool UpdateInit(long pdemReportId,
                               out string monPlanId,
                               out int? rptPeriodId,
                               out bool? isMatsEmissionReport,
                               out DataTable locationTable,
                               out DataTable rptPeriodInfoTable,
                               out DataTable locationTypeCountTable,
                               out DataTable locationLinkSpanCountTable,
                               out DataTable locationLinkActiveTable,
                               out DataTable specialMethodCountTable,
                               out DataTable monitorHourTable,
                               ref string errorMessage)
        {
            bool result;

            try
            {
                Db.CreateTextCommand("call camdecmpsaux.PDEM_UPDATE_INIT( $1, $2, $3, $4, $5, $6 )");

                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Bigint).Value = pdemReportId.DbValue();
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Varchar).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Numeric).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Boolean).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Boolean).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Text).Value = DBNull.Value;

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(Db.Command);
                DataTable dataTable = new DataTable("ResultsTable");
                adapter.Fill(dataTable);

                monPlanId = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][0].AsString() : null;
                rptPeriodId = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][1].AsInteger() : null;
                isMatsEmissionReport = (dataTable.Rows.Count > 0) ? bool.Parse(dataTable.Rows[0][2].AsString()) : null;
                result = (dataTable.Rows.Count > 0) ? bool.Parse(dataTable.Rows[0][3].AsString()) : false;
                errorMessage = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][4].AsString() : $"Result row not returned by '{Db.Command.CommandText}'";

                if (result)
                {
                    locationTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_LOCATION_INFO( '{monPlanId}' )");
                    rptPeriodInfoTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_REPORTING_PERIOD_INFO( {rptPeriodId} )");
                    locationTypeCountTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_LOCATION_TYPE_COUNTS( '{monPlanId}' )");
                    locationLinkSpanCountTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_SPANNING_LINK_COUNTS( '{monPlanId}', {rptPeriodId} )");
                    locationLinkActiveTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_ACTIVE_LINK_COUNTS( '{monPlanId}', {rptPeriodId} )");
                    specialMethodCountTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_CP_LTFF_INFO( '{monPlanId}', {rptPeriodId} )");
                    monitorHourTable = Db.GetDataTable($"select * from camdecmpsaux.PDEM_UPDATE_INIT_GET_COMBINED_HOURLY_DATA( '{monPlanId}', {rptPeriodId} )");
                }
                else
                {
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                monPlanId = null;
                rptPeriodId = null;
                isMatsEmissionReport = null;
                locationTable = null;
                rptPeriodInfoTable = null;
                locationTypeCountTable = null;
                locationLinkSpanCountTable = null;
                locationLinkActiveTable = null;
                specialMethodCountTable = null;
                monitorHourTable = null;

                _logger?.LogError(ex, "PDEM.UpdateInit({pdemReportId})", pdemReportId);

                errorMessage = ex.Message;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Update the check log, set apportionment type, and set emissions created to 'N'.
        /// </summary>
        /// <param name="pdemReportId">The PDEM_REPORT_ID of the update.</param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the SP executed without error.</returns>
        public bool UpdatePublic(long pdemReportId,
                                 out string errorMessage)
        {
            errorMessage = null;
            bool result;

            try
            {
                Db.CreateTextCommand("call camdecmpsaux.PDEM_UPDATE_PUBLIC( $1, $2, $3 )");

                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Bigint).Value = pdemReportId.DbValue();
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Boolean).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Varchar).Value = DBNull.Value;

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(Db.Command);
                DataTable dataTable = new DataTable("ResultsTable");
                adapter.Fill(dataTable);

                result = (dataTable.Rows.Count > 0) ? bool.Parse(dataTable.Rows[0][0].AsString()) : false;

                if (!result)
                {
                    errorMessage = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][1].AsString() : $"Result row not returned by '{Db.Command.CommandText}'";
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDEM.UpdatePublic({pdemReportId})", pdemReportId);
                errorMessage = ex.Message;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Delegate for the method called to update the apportionment type, unit hour data 
        /// and check log.
        /// </summary>
        /// <param name="pdemReportId">The PDEM_REPORT_ID of the update.</param>
        /// <param name="apportionmentType">The apportionment type of the update.</param>
        /// <param name="isMatsEmissionReport">Indicates whether the emission report is a MATS report.</param>
        /// <param name="errorMessage">The error message indicating why the updated failed.</param>
        public bool UpdateSuccess(long pdemReportId,
                                  eApportionmentType? apportionmentType,
                                  bool? isMatsEmissionReport,
                                  out string errorMessage)
        {
            bool result = true;
            errorMessage = null;

            try
            {
                Db.CreateTextCommand("call camdecmpsaux.PDEM_UPDATE_SUCCESS( $1, $2, $3, $4 )");

                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Bigint).Value = pdemReportId.DbValue();
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Varchar).Value = ((apportionmentType != null) ? apportionmentType?.DbCode() : DBNull.Value);
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Boolean).Value = DBNull.Value;
                Db.Command.Parameters.Add(null, NpgsqlTypes.NpgsqlDbType.Text).Value = DBNull.Value;

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(Db.Command);
                DataTable dataTable = new DataTable("ResultsTable");
                adapter.Fill(dataTable);

                result = (dataTable.Rows.Count > 0) ? bool.Parse(dataTable.Rows[0][0].AsString()) : false;
                errorMessage = (dataTable.Rows.Count > 0) ? dataTable.Rows[0][1].AsString() : $"Result row not returned by '{Db.Command.CommandText}'";

                if (!result)
                {
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDEM.UpdateSuccess({pdemReportId}, {apportionmentType}, {isMatsEmissionReport})", pdemReportId, apportionmentType, isMatsEmissionReport);
                errorMessage = ex.Message;
                result = false;
            }

            return result;
        }


        /// <summary>
        /// Updates UNIT_HOUR with the apportioned data.
        /// </summary>
        /// <param name="isMatsEmissionReport">Indicates whether the emission report is a MATS report.</param>
        /// <param name="hourlyApportionedData">The apportioned data.</param>
        /// <param name="errorMessage">The error message indicating why the updated failed.</param>
        /// <returns>True if successful.</returns>
        public bool UpdateUnitHourData(bool? isMatsEmissionReport,
                                       cHourlyApportionedData hourlyApportionedData,
                                       out string errorMessage)
        {
            bool result;

            long[] pdemReportIdArray = hourlyApportionedData.PdemReportIdArray;
            int?[] unitKeyArray = hourlyApportionedData.UnitKeyArray;
            DateTime?[] opDateArray = hourlyApportionedData.OpDateArray;
            int?[] opHourArray = hourlyApportionedData.OpHourArray;
            decimal?[] opTimeArray = hourlyApportionedData.OpTimeArray;
            decimal?[] gLoadArray = hourlyApportionedData.GLoadArray;
            decimal?[] mLoadArray = hourlyApportionedData.MLoadArray;
            decimal?[] sLoadArray = hourlyApportionedData.SLoadArray;
            decimal?[] tLoadArray = hourlyApportionedData.TLoadArray;
            decimal?[] hitArray = hourlyApportionedData.HitArray;
            string[] hitMeasureArray = hourlyApportionedData.HitMeasureArray;
            decimal?[] so2mArray = hourlyApportionedData.So2mArray;
            string[] so2mMeasureArray = hourlyApportionedData.So2mMeasureArray;
            decimal?[] so2rArray = hourlyApportionedData.So2rArray;
            string[] so2rMeasureArray = hourlyApportionedData.So2rMeasureArray;
            decimal?[] co2mArray = hourlyApportionedData.Co2mArray;
            string[] co2mMeasureArray = hourlyApportionedData.Co2mMeasureArray;
            decimal?[] co2rArray = hourlyApportionedData.Co2rArray;
            string[] co2rMeasureArray = hourlyApportionedData.Co2rMeasureArray;
            decimal?[] noxmArray = hourlyApportionedData.NoxmArray;
            string[] noxmMeasureArray = hourlyApportionedData.NoxmMeasureArray;
            decimal?[] noxrArray = hourlyApportionedData.NoxrArray;
            string[] noxrMeasureArray = hourlyApportionedData.NoxrMeasureArray;
            decimal?[] hgRateEoArray = hourlyApportionedData.HgRateEoArray;
            decimal?[] hgRateHiArray = hourlyApportionedData.HgRateHiArray;
            decimal?[] hgMassArray = hourlyApportionedData.HgMassArray;
            string[] hgMeasureArray = hourlyApportionedData.HgMeasureArray;
            decimal?[] hclRateEoArray = hourlyApportionedData.HclRateEoArray;
            decimal?[] hclRateHiArray = hourlyApportionedData.HclRateHiArray;
            decimal?[] hclMassArray = hourlyApportionedData.HclMassArray;
            string[] hclMeasureArray = hourlyApportionedData.HclMeasureArray;
            decimal?[] hfRateEoArray = hourlyApportionedData.HfRateEoArray;
            decimal?[] hfRateHiArray = hourlyApportionedData.HfRateHiArray;
            decimal?[] hfMassArray = hourlyApportionedData.HfMassArray;
            string[] hfMeasureArray = hourlyApportionedData.HfMeasureArray;
            string[] monPlanIdArray = hourlyApportionedData.MonPlanIdArray;
            int?[] rptPeriodIdArray = hourlyApportionedData.RptPeriodIdArray;
            int?[] opYearArray = hourlyApportionedData.OpYearArray;

            try
            {
                if (unitKeyArray.Length > 0)
                {
                    DataTable unitHourTable = Db.GetDataTable("SELECT * FROM camdecmpsaux.PDEM_P75_UNIT_HOUR WHERE NULL = NULL");
                    DataTable matsUnitHourTable = Db.GetDataTable("SELECT * FROM camdecmpsaux.PDEM_MATS_UNIT_HOUR WHERE NULL = NULL");

                    for (int dex = 0; dex < unitKeyArray.Length; dex++)
                    {
                        DataRow unitHourRow = unitHourTable.NewRow();
                        {
                            unitHourRow["PDEM_REPORT_ID"] = pdemReportIdArray[dex].DbValue();
                            unitHourRow["UNIT_ID"] = unitKeyArray[dex].DbValue();
                            unitHourRow["OP_DATE"] = opDateArray[dex].DbValue();
                            unitHourRow["OP_HOUR"] = opHourArray[dex].DbValue();
                            unitHourRow["OP_TIME"] = opTimeArray[dex].DbValue();
                            unitHourRow["GLOAD"] = gLoadArray[dex].DbValue();
                            unitHourRow["SLOAD"] = sLoadArray[dex].DbValue();
                            unitHourRow["TLOAD"] = tLoadArray[dex].DbValue();
                            unitHourRow["HIT"] = hitArray[dex].DbValue();
                            unitHourRow["HIT_HOUR_MEASURE_CD"] = hitMeasureArray[dex].DbValue();
                            unitHourRow["SO2M"] = so2mArray[dex].DbValue();
                            unitHourRow["SO2M_HOUR_MEASURE_CD"] = so2mMeasureArray[dex].DbValue();
                            unitHourRow["SO2R"] = so2rArray[dex].DbValue();
                            unitHourRow["SO2R_HOUR_MEASURE_CD"] = so2rMeasureArray[dex].DbValue();
                            unitHourRow["CO2M"] = co2mArray[dex].DbValue();
                            unitHourRow["CO2M_HOUR_MEASURE_CD"] = co2mMeasureArray[dex].DbValue();
                            unitHourRow["CO2R"] = co2rArray[dex].DbValue();
                            unitHourRow["CO2R_HOUR_MEASURE_CD"] = co2rMeasureArray[dex].DbValue();
                            unitHourRow["NOXM"] = noxmArray[dex].DbValue();
                            unitHourRow["NOXM_HOUR_MEASURE_CD"] = noxmMeasureArray[dex].DbValue();
                            unitHourRow["NOXR"] = noxrArray[dex].DbValue();
                            unitHourRow["NOXR_HOUR_MEASURE_CD"] = noxrMeasureArray[dex].DbValue();
                            unitHourRow["MON_PLAN_ID"] = monPlanIdArray[dex].DbValue();
                            unitHourRow["RPT_PERIOD_ID"] = rptPeriodIdArray[dex].DbValue();
                            unitHourRow["OP_YEAR"] = opYearArray[dex].DbValue();
                        }
                        unitHourTable.Rows.Add(unitHourRow);

                        if (isMatsEmissionReport.Default(false))
                        {
                            DataRow matsUnitHourRow = matsUnitHourTable.NewRow();
                            {
                                matsUnitHourRow["PDEM_REPORT_ID"] = pdemReportIdArray[dex].DbValue();
                                matsUnitHourRow["UNIT_ID"] = unitKeyArray[dex].DbValue();
                                matsUnitHourRow["OP_DATE"] = opDateArray[dex].DbValue();
                                matsUnitHourRow["OP_HOUR"] = opHourArray[dex].DbValue();
                                matsUnitHourRow["OP_TIME"] = opTimeArray[dex].DbValue();
                                matsUnitHourRow["GLOAD"] = mLoadArray[dex].DbValue();
                                matsUnitHourRow["SLOAD"] = sLoadArray[dex].DbValue();
                                matsUnitHourRow["TLOAD"] = tLoadArray[dex].DbValue();
                                matsUnitHourRow["HIT"] = hitArray[dex].DbValue();
                                matsUnitHourRow["HIT_HOUR_MEASURE_CD"] = hitMeasureArray[dex].DbValue();
                                matsUnitHourRow["HG_RATE_EO"] = hgRateEoArray[dex].DbValue();
                                matsUnitHourRow["HG_RATE_HI"] = hgRateHiArray[dex].DbValue();
                                matsUnitHourRow["HG_MASS"] = hgMassArray[dex].DbValue();
                                matsUnitHourRow["HG_HOUR_MEASURE_CD"] = hgMeasureArray[dex].DbValue();
                                matsUnitHourRow["HCL_RATE_EO"] = hclRateEoArray[dex].DbValue();
                                matsUnitHourRow["HCL_RATE_HI"] = hclRateHiArray[dex].DbValue();
                                matsUnitHourRow["HCL_MASS"] = hclMassArray[dex].DbValue();
                                matsUnitHourRow["HCL_HOUR_MEASURE_CD"] = hclMeasureArray[dex].DbValue();
                                matsUnitHourRow["HF_RATE_EO"] = hfRateEoArray[dex].DbValue();
                                matsUnitHourRow["HF_RATE_HI"] = hfRateHiArray[dex].DbValue();
                                matsUnitHourRow["HF_MASS"] = hfMassArray[dex].DbValue();
                                matsUnitHourRow["HF_HOUR_MEASURE_CD"] = hfMeasureArray[dex].DbValue();
                                matsUnitHourRow["MON_PLAN_ID"] = monPlanIdArray[dex].DbValue();
                                matsUnitHourRow["RPT_PERIOD_ID"] = rptPeriodIdArray[dex].DbValue();
                                matsUnitHourRow["OP_YEAR"] = opYearArray[dex].DbValue();
                            }
                            matsUnitHourTable.Rows.Add(matsUnitHourRow);
                        }
                    }

                    errorMessage = null;
                    result = true;

                    if (!Db.BulkLoad(unitHourTable,
                                     "camdecmpsaux.PDEM_P75_UNIT_HOUR",
                                     new string[] { "PDEM_P75_UNIT_HOUR_ID", "ADD_DATE" },
                                     ref errorMessage))
                    {
                        throw new Exception($"{errorMessage} (P75 Unit Hourly BulkLoad)");
                    }

                    if (result && isMatsEmissionReport.Default(false))
                    {
                        if (!Db.BulkLoad(matsUnitHourTable,
                                        "camdecmpsaux.PDEM_MATS_UNIT_HOUR",
                                        new string[] { "PDEM_MATS_UNIT_HOUR_ID", "ADD_DATE" },
                                        ref errorMessage))
                        {
                            throw new Exception($"{errorMessage} (MATS Unit Hourly BulkLoad)");
                        }
                    }
                }
                else
                {
                    errorMessage = null;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDEM.UpdateUnitHour({isMatsEmissionReport})", isMatsEmissionReport);
                errorMessage = ex.Message;
                result = false;
            }

            return result;
        }

        #endregion

    }
}
