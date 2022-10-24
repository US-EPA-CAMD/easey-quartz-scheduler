using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace ECMPS.Checks.DatabaseAccess
{
    
    /// <summary>
    /// Replaces DataDBDataContext DBML, especially the method calls for stored procedures/functions
    /// </summary>
    public class DataDBDataContext
    {

        /// <summary>
        /// Creates a DataDBDataContext instance using the cDatabase instance and command timeout defaulted to 30 seconds.
        /// </summary>
        /// <param name="database">The cDatabase instance for the ECMPS (data) database.</param>
        /// <param name="commandTimeout">The default timeout to use for the database.</param>
        public DataDBDataContext(cDatabase database, int commandTimeout = 30)
        {
            Database = database;
            CommandTimeout = commandTimeout;
        }
        
        /// <summary>
        /// The command timeout to use for database commands.
        /// </summary>
        public int CommandTimeout { get; set; }

        /// <summary>
        /// The cDatabase instance to use for datbase connections.
        /// </summary>
        public cDatabase Database { get; private set; }

        
        /// <summary>
        /// Calls ECMPS.Check.GetFacilityInfo stored procedure.
        /// </summary>
        /// <param name="lookupType">Lookup Id Type: MP (MON_PLAN_ID), TEST (TEST_SUM_ID), QAC (QA_CERT_EVENT), TEE ()TEST_EXTENSION_EXEMPTION_ID) and ORIS (ORIS_CODE).</param>
        /// <param name="lookupId">Lookup Id to use in the lookup.</param>
        /// <param name="facId">FAC_ID for the facility.</param>
        /// <param name="firstEcmpsRptPeriodId">First ECMPS RPT_PERIOD_ID for the facility.</param>
        /// <param name="errorMessage">Message returned if the get failed.</param>
        /// <returns></returns>
        public int GetFacilityInfo(string lookupType, string lookupId, ref System.Nullable<int> facId, ref System.Nullable<int> firstEcmpsRptPeriodId, ref string errorMessage)
        {
            //TODO (EC-3519): Testing Needed
            DataTable AResultTable;
            string Sql = "SELECT * FROM  camdecmpswks.get_facility_info('" + lookupType + "','" + lookupId + "')";

            try
            {
                AResultTable = Database.GetDataTable(Sql);

                foreach (DataRow row in AResultTable.Rows)
                {
                    facId = int.Parse(row["facId"].ToString());
                    firstEcmpsRptPeriodId = (row["firstEcmpsRptPeriodId"] != DBNull.Value) ? int.Parse(row["firstEcmpsRptPeriodId"].ToString()) : (int?)null; // Must handle a null firstEcmpsrptPeriodId
                    errorMessage = row["error_msg"].ToString();
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
           //Database.CreateStoredProcedureCommand("camdecmpswks.GetFacilityInfo");

            //Database.AddInputParameter("@lookupType", lookupType);
            //Database.AddInputParameter("@lookupid", lookupId);
            //Database.AddOutputParameterInt("@facId");
            //Database.AddOutputParameterInt("@firstEcmpsRptPeriodId");
            //Database.AddOutputParameterString("@errorMessage", 1000);

            //Database.ExecuteNonQuery();

            //facId = Database.GetParameterInt("@facId");
            //firstEcmpsRptPeriodId = Database.GetParameterInt("@firstEcmpsRptPeriodId");
            //errorMessage = Database.GetParameterString("@errorMessage");

            //SQL Server Version
            //Database.CreateStoredProcedureCommand("Check.GetFacilityInfo");

            //Database.AddInputParameter("@lookupType", lookupType);
            //Database.AddInputParameter("@lookupid", lookupId);
            //Database.AddOutputParameterInt("@facId");
            //Database.AddOutputParameterInt("@firstEcmpsRptPeriodId");
            //Database.AddOutputParameterString("@errorMessage", 1000);

            //Database.ExecuteNonQuery();

            //facId = Database.GetParameterInt("@facId");
            //firstEcmpsRptPeriodId = Database.GetParameterInt("@firstEcmpsRptPeriodId");
            //errorMessage = Database.GetParameterString("@errorMessage");

            return 0;
        }

        /// <summary>
        /// Calls ECMPS.CheckMp.GetInitialValues stored procedure.
        /// </summary>
        /// <param name="monPlanId">MON_PLAN_ID of the monitoring plan being evaluated.</param>
        /// <param name="defaultEvaluationEndDate">The default Evaluation End Date (previously determined in ECMPS_Client solution.</param>
        /// <param name="evaluationEndDate">The actual Evaluation End Date to use in the evaluation.</param>
        /// <param name="maximumFutureDate">The begin date for some monitoring plan elements beyond which they are not evaluated.</param>
        /// <param name="result">T if the SP ran successfully, F if it did not.</param>
        /// <param name="errorMessage">The message returned when the SP did not run successfully.</param>
        /// <returns>0</returns>
        public int GetInitialValues(string monPlanId, System.Nullable<System.DateTime> defaultEvaluationEndDate, ref System.Nullable<System.DateTime> evaluationEndDate, ref System.Nullable<System.DateTime> maximumFutureDate, ref System.Nullable<char> result, ref string errorMessage)
        {
            //TODO (EC-3519): Testing Needed
            string resultString = string.Empty;
         
            DateTime db = new DateTime(2016, 12, 3); //TESTTIN VALUE
            DataTable AResultTable;
            string Sql = "SELECT * FROM  camdecmpswks.get_initial_values('" + monPlanId + "','" + defaultEvaluationEndDate + "')";

            try
            {
                AResultTable = Database.GetDataTable(Sql);

                foreach (DataRow row in AResultTable.Rows)
                {
                    evaluationEndDate = DateTime.Parse(row["evaluationEndDate"].ToString());
                    maximumFutureDate = DateTime.Parse(row["maximumFutureDate"].ToString());
                    resultString = row["result"].ToString();
                    errorMessage = row["errorMessage"].ToString();
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            //Database.CreateStoredProcedureCommand("camdecmpswks.get_initial_values");

            //Database.AddInputParameter("@monPlanID", monPlanId);
            //Database.AddInputParameter("@defaultEvaluationEndDate", db.ToString("yyyy-MM-dd"));
            //Database.AddOutputParameterDateTime("@evaluationEndDate");
            //Database.AddOutputParameterDateTime("@maximumFutureDate");
            //Database.AddOutputParameterString("@result", 1);
            //Database.AddOutputParameterString("@errorMessage", -1);

            //Database.ExecuteNonQuery();

            //evaluationEndDate = Database.GetParameterDateTime("@evaluationEndDate");
            //maximumFutureDate = Database.GetParameterDateTime("@maximumFutureDate");
            //resultString = Database.GetParameterString("@result");
            //errorMessage = Database.GetParameterString("@errorMessage");

            //SQL Server Version
            //Database.CreateStoredProcedureCommand("CheckMp.GetInitialValues");

            //Database.AddInputParameter("@monPlanID", monPlanId);
            //Database.AddInputParameter("@defaultEvaluationEndDate", defaultEvaluationEndDate);
            //Database.AddOutputParameterDateTime("@evaluationEndDate");
            //Database.AddOutputParameterDateTime("@maximumFutureDate");
            //Database.AddOutputParameterString("@result", 1);
            //Database.AddOutputParameterString("@errorMessage", -1);

            //Database.ExecuteNonQuery();

            //evaluationEndDate = Database.GetParameterDateTime("@evaluationEndDate");
            //maximumFutureDate = Database.GetParameterDateTime("@maximumFutureDate");
            //resultString = Database.GetParameterString("@result");
            //errorMessage = Database.GetParameterString("@errorMessage");

            result = !string.IsNullOrWhiteSpace(resultString) ? resultString[0] : (char?)null;

            return 0;
        }

        /// <summary>
        /// Calls ECMPS.Check.GetReportPeriodInfo stored procedure.
        /// </summary>
        /// <param name="rptPeriodId">The RPT_PERIOD_ID of the reporting period information to return.</param>
        /// <param name="calendarYear">The calendar year of the reporting period.</param>
        /// <param name="quarter">The quarter of the reporting period.</param>
        /// <param name="periodDescription_in">The description of the reporting period.</param>
        /// <param name="periodAbbreviation">The abbreviated description of the reporting period.</param>
        /// <param name="beginDate">The begin date of the reporting period.</param>
        /// <param name="endDate">The end date of the reporting period.</param>
        /// <param name="result">T if the SP ran successfully, F if it did not.</param>
        /// <param name="errorMessage">The message returned when the SP did not run successfully.</param>
        /// <returns></returns>
        public int GetReportPeriodInfo(System.Nullable<int> rptPeriodId, ref System.Nullable<int> calendarYear, ref System.Nullable<int> quarter, ref string periodDescription_in, ref string periodAbbreviation, ref System.Nullable<System.DateTime> beginDate, ref System.Nullable<System.DateTime> endDate, ref System.Nullable<char> result, ref string errorMessage)
        {
            //TODO (EC-3519): Testing Needed
            string resultString = string.Empty;

            DataTable AResultTable;
            string Sql = "SELECT * FROM  camdecmpswks.get_report_period_info('" + rptPeriodId + "')";

            try
            {
                AResultTable = Database.GetDataTable(Sql);

                foreach (DataRow row in AResultTable.Rows)
                {
                    calendarYear = int.Parse(row["p_calendarYear_in"].ToString());
                    quarter = int.Parse(row["p_quarter_in"].ToString());
                    periodDescription_in = row["p_periodDescription_in"].ToString();
                    periodAbbreviation = row["p_periodAbbreviation_in"].ToString();
                    beginDate = DateTime.Parse(row["p_beginDate"].ToString());
                    endDate = DateTime.Parse(row["p_endDate"].ToString());
                    resultString = row["p_result"].ToString();
                    errorMessage = row["p_errorMessage"].ToString();
                   
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }



            //Database.CreateStoredProcedureCommand("camdecmpswks.get_report_period_info");

            //Database.AddInputParameter("@p_rptperiodid_in", rptPeriodId);
            //Database.AddOutputParameterInt("@p_calendarYear_in");
            //Database.AddOutputParameterInt("@qp_uarter_in");
            //Database.AddOutputParameterString("@p_periodDescription_in", 100);
            //Database.AddOutputParameterString("@p_periodAbbreviation_in", 32);
            //Database.AddOutputParameterDateTime("@p_beginDate");
            //Database.AddOutputParameterDateTime("@p_endDate");
            //Database.AddOutputParameterString("@p_result", 1);
            //Database.AddOutputParameterString("@p_errorMessage", 1000);

            //Database.ExecuteNonQuery();

            //calendarYear = Database.GetParameterInt("@p_calendarYear_in");
            //quarter = Database.GetParameterInt("@p_quarter_in");
            //periodDescription_in = Database.GetParameterString("@p_periodDescription_in");
            //periodAbbreviation = Database.GetParameterString("@p_periodAbbreviation_in");
            //beginDate = Database.GetParameterDateTime("@p_beginDate");
            //endDate = Database.GetParameterDateTime("@p_endDate");
            //resultString = Database.GetParameterString("@p_result");
            //errorMessage = Database.GetParameterString("@p_errorMessage");

            //SQL Server Version
            //Database.CreateStoredProcedureCommand("Check.GetReportPeriodInfo");

            //Database.AddInputParameter("@rptPeriodId", rptPeriodId);
            //Database.AddOutputParameterInt("@calendarYear_in");
            //Database.AddOutputParameterInt("@quarter_in");
            //Database.AddOutputParameterString("@periodDescription_in", 100);
            //Database.AddOutputParameterString("@periodAbbreviation_in", 32);
            //Database.AddOutputParameterDateTime("@beginDate");
            //Database.AddOutputParameterDateTime("@endDate");
            //Database.AddOutputParameterString("@result", 1);
            //Database.AddOutputParameterString("@errorMessage", 1000);

            //Database.ExecuteNonQuery();

            //calendarYear = Database.GetParameterInt("@calendarYear_in");
            //quarter = Database.GetParameterInt("@quarter_in");
            //periodDescription_in = Database.GetParameterString("@periodDescription_in");
            //periodAbbreviation = Database.GetParameterString("@periodAbbreviation_in");
            //beginDate = Database.GetParameterDateTime("@beginDate");
            //endDate = Database.GetParameterDateTime("@endDate");
            //resultString = Database.GetParameterString("@result");
            //errorMessage = Database.GetParameterString("@errorMessage");

            result = !string.IsNullOrWhiteSpace(resultString) ? resultString[0] : (char?)null;

            return 0;
        }

    }
}
