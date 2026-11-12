using Epa.Camd.Quartz.Scheduler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Quartz;
using SilkierQuartz;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading.Tasks;


namespace Epa.Camd.Quartz.Scheduler.Jobs
{

    public class DataMartRefreshJob(NpgSqlContext dbContext, ILogger<DataMartRefreshJob> logger) : IJob, IJobMetadata<DataMartRefreshJob>
    {
        #region Public Static Variables

        public static string JobGroup => Constants.QuartzGroups.DATA_MART_REFRESH;
        public static string JobName => "Data Mart Refresh Job";
        public static string JobDescription => "Refreshes the Data Mart tables in the CAMDDMW database schema.";

        public static string TriggerName => "Data Mart Refresh Trigger";
        public static string TriggerDescription => "Trigger to run the Data Mart Refresh Job, usually nightly.";

        #endregion


        #region Public Properties

        public IConfiguration Configuration { get; }

        #endregion


        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                JobLog jobLog = await JobLogCreate();

                try
                {
                    logger.LogInformation("{JobName} job starting. [{JobId}]", JobName, jobLog.JobId);

                    // Check if job is already running
                    if (IsJobInProgress())
                    {
                        logger.LogInformation("{JobName} is already in progress. Skipping execution. Job ID: {JobId}", JobName, jobLog.JobId);
                        return;
                    }

                    CallDataMartRefreshes();

                    await JobLogMarkSuccessful(jobLog);

                    logger.LogInformation("{JobName} job completed successfully. Job ID: {JobId}", JobName, jobLog.JobId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{JobName} produced an error during execution. [{JobId}]", JobName, jobLog.JobId);
                    await JobLogMarkFailed(jobLog, ex.Message);
                    throw;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "{JobName} job was unable to create Job Log row.", JobName);
                throw;
            }
        }


        /// <summary>
        /// Calls the following camddmw stored procedures,
        /// 
        ///     1. camdsnap.refresh_materialized_views
        ///     2. camddmw.dm_refresh_update_supporting_data
        ///     3. camddmw.dm_refresh_get_start_info
        ///     4. camddmw.dm_refresh_all_allowance_data
        ///     5. camddmw.dm_refresh_all_compliance_data
        ///     6. camddmw.dm_refresh_all_unit_data
        ///     
        /// Throws an exception if any of the stored procedure throws an exception.
        /// </summary>
        private void CallDataMartRefreshes()
        {
            logger.LogInformation("Calling Data Mart refresh stored procedures.");

            int? dmRefreshLogId = null;

            try
            {
                if (!CallDataMartRefreshes_LogStart(out dmRefreshLogId))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_get_start_info. See logs for details.");
                }

                CallDataMartRefreshes_Snapshots();

                if (!CallDataMartRefreshes_UpdateSupportingData())
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_update_supporting_data. See logs for details.");
                }

                if (!CallDataMartRefreshes_GetStartInfo(dmRefreshLogId.Value, out bool? isFullRefresh, out DateOnly? lookbackDate))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_get_start_info. See logs for details.");
                }

                if (!CallDataMartRefreshes_ForDataGroup("camddmw", "dm_refresh_all_unit_data", isFullRefresh, lookbackDate))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_all_unit_data. See logs for details.");
                }

                if (!CallDataMartRefreshes_ForDataGroup("camddmw", "dm_refresh_all_allowance_data", isFullRefresh, lookbackDate))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_all_allowance_data. See logs for details.");
                }

                if (!CallDataMartRefreshes_ForDataGroup("camddmw", "dm_refresh_all_compliance_data", isFullRefresh, lookbackDate))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_all_compliance_data. See logs for details.");
                }

                if (!CallDataMartRefreshes_LogComplete(dmRefreshLogId.Value))
                {
                    throw new Exception("Error occurred while executing camddmw.dm_refresh_log_complete. See logs for details.");
                }

                logger.LogInformation("Completed Data Mart refresh stored procedures for log ID {DmRefreshLogId}.", dmRefreshLogId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing Data Mart refresh stored procedures.");

                if (dmRefreshLogId.HasValue && !CallDataMartRefreshes_LogFailed(dmRefreshLogId.Value, ex))
                {
                    logger.LogError(ex, "Error occurred while logging Data Mart refresh failure for log ID {DmRefreshLogId}.", dmRefreshLogId.Value );
                }

                throw;
            }
        }


        /// <summary>
        /// Calls camdsnap.refresh_materialized_views to refresh the CAMDSNAP materialized views.
        /// Throws an exception if the stored procedure throws an exception.
        /// </summary>
        private void CallDataMartRefreshes_Snapshots()
        {
            const string storedProcedureFullName = "camdsnap.refresh_materialized_views";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            try
            {
                var command = dbContext.ExecuteProcedure(storedProcedureFullName, new ());
                logger.LogInformation("{StoredProcedure} completed.", storedProcedureFullName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{StoredProcedure} failed.", storedProcedureFullName);
                throw;
            }
        }


        /// <summary>
        /// Calls the stored procedure for the passed storedProcedureSchema and storedProcedureName values, passing in the isFullRefresh and lookbackDate parameters.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - isfullrefresh_out (boolean, input)
        ///     - lookbackdate_out (date, input)
        ///     - errorjson_out (json, output)
        ///     - result_out (boolean, output)
        /// </summary>
        /// <param name="storedProcedureSchema">The schema of the stored procedure.</param>
        /// <param name="storedProcedureName">The name of the stored procedure.</param>
        /// <param name="isFullRefresh">Indicates whether to perform a full refresh.</param>
        /// <param name="lookbackDate">The lookback date for an incremental refresh.</param>
        /// <returns>True if the stored procedure executed successfully; otherwise, false.  </returns>
        /// <exception cref="Exception"></exception>
        private bool CallDataMartRefreshes_ForDataGroup(string storedProcedureSchema, string storedProcedureName, bool? isFullRefresh, DateOnly? lookbackDate)
        {
            string storedProcedureFullName = storedProcedureSchema + "." + storedProcedureName;

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var isFullRefreshInParameter = dbContext.CreateParameter("fullrefresh_in", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Input);
            var lookbackDateInParameter = dbContext.CreateParameter("increamentalrefreshdate_in", null, NpgsqlDbType.Date, System.Data.ParameterDirection.Input);
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Set the input parameter values, using DBNull.Value for nulls
            isFullRefreshInParameter.Value = isFullRefresh ?? (object)DBNull.Value;
            lookbackDateInParameter.Value = lookbackDate ?? (object)DBNull.Value;

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { isFullRefreshInParameter, lookbackDateInParameter, errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool?)resultOutParameter.Value : null;

            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }
            else
            {
                logger.LogInformation("{StoredProcedure} completed successfully.", storedProcedureFullName);
            }

            return result ?? false;
        }


        /// <summary>
        /// Calls the camddmw.dm_refresh_get_start_info stored procedure and returns the isFullRefresh and lookbackDate output parameters from the stored procedure.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - dmRefreshLogId_in (input parameter of type integer)
        ///     - isfullrefresh_out (output parameter of type boolean)
        ///     - lookbackdate_out (output parameter of type date)
        ///     - errorjson_out (output parameter of type json)
        ///     - result_out (output parameter of type boolean)
        /// </summary>
        /// <param name="dmRefreshLogId">Primary key of the data mart refresh log.</param>
        /// <param name="isFullRefresh">Output parameter that indicates whether a full refresh is required.</param>
        /// <param name="lookbackDate">Output parameter that provides the lookback date.</param>
        /// <returns><see langword="true"/> when the stored procedure returns a successful result; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown when <c>result_out</c> is missing or contains an unexpected value instead of a boolean success indicator.</exception>
        private bool CallDataMartRefreshes_GetStartInfo(int dmRefreshLogId, out bool? isFullRefresh, out DateOnly? lookbackDate)
        {
            const string storedProcedureFullName = "camddmw.dm_refresh_get_start_info";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var dmRefreshLogIdInParameter = dbContext.CreateParameter("dmrefreshlogid_in", dmRefreshLogId.ToString(), NpgsqlDbType.Integer, System.Data.ParameterDirection.Input);
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var isFullRefreshOutParameter = dbContext.CreateParameter("isfullrefresh_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);
            var lookbackDateOutParameter = dbContext.CreateParameter("lookbackdate_out", null, NpgsqlDbType.Date, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Set the input parameter values, using DBNull.Value for nulls
            dmRefreshLogIdInParameter.Value = dmRefreshLogId;

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { dmRefreshLogIdInParameter, isFullRefreshOutParameter, lookbackDateOutParameter, errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            isFullRefresh = isFullRefreshOutParameter.Value != DBNull.Value ? (bool?)isFullRefreshOutParameter.Value : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool?)resultOutParameter.Value : null;

            // Should handle default type change for PostgreSQL Date type in Npgsql version 10.
            
            
            lookbackDate = lookbackDateOutParameter.Value switch
            {
                DateTime datatime => DateOnly.FromDateTime(datatime),
                DateOnly dataonly => dataonly,
                _ => null
            };


            logger.LogInformation("{StoredProcedure} completed with result: {Result}", storedProcedureFullName, Convert.ToString(result) ?? "null");

            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }
            else if (isFullRefresh == null)
            {
                // Unexpected isFullRefresh value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Is Full Refresh value: {IsFullRefresh}. Expected 'True' or 'False'.",
                                 storedProcedureFullName, Convert.ToString(isFullRefresh) ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Is Full Refresh value: '{Convert.ToString(isFullRefresh) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (isFullRefresh.Value)
            {
                logger.LogInformation("{StoredProcedure} indicates a full refresh is required.", storedProcedureFullName);
            }
            else if (lookbackDate == null)
            {
                // Unexpected lookbackDate value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Lookback Date value: {LookbackDate}. Expected a valid date.",
                                 storedProcedureFullName, Convert.ToString(lookbackDate) ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Lookback Date value: '{Convert.ToString(lookbackDate) ?? "null"}'. Expected a valid date.");
            }
            else
            {
                logger.LogInformation("{StoredProcedure} indicates an incremental refresh is required. Lookback date: {LookbackDate}", storedProcedureFullName, lookbackDate?.ToString() ?? "null");
            }

            return result ?? false;
        }


        /// <summary>
        /// Calls the camddmw.dm_refresh_log_complete stored procedure.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - dmRefreshLogId_in (input parameter of type integer)
        ///     - errorjson_out (output parameter of type json)
        ///     - result_out (output parameter of type boolean)
        /// </summary>
        /// <param name="dmRefreshLogId">Primary key of the data mart refresh log.</param>
        /// <returns><see langword="true"/> when the stored procedure returns a successful result; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown when <c>result_out</c> is missing or contains an unexpected value instead of a boolean success indicator.</exception>
        private bool CallDataMartRefreshes_LogComplete(int dmRefreshLogId)
        {
            const string storedProcedureFullName = "camddmw.dm_refresh_log_complete";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var dmRefreshLogIdInParameter = dbContext.CreateParameter("dmrefreshlogid_in", dmRefreshLogId.ToString(), NpgsqlDbType.Integer, System.Data.ParameterDirection.Input);
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Set the input parameter values, using DBNull.Value for nulls
            dmRefreshLogIdInParameter.Value = dmRefreshLogId;

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { dmRefreshLogIdInParameter, errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool?)resultOutParameter.Value : null;


            logger.LogInformation("{StoredProcedure} completed with result: {Result}", storedProcedureFullName, Convert.ToString(result) ?? "null");

            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }

            return result ?? false;
        }


        /// <summary>
        /// Calls the camddmw.dm_refresh_log_failed stored procedure.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - dmRefreshLogId_in (input parameter of type integer)
        ///     - errorJson_in (input parameter of type json)
        ///     - errorjson_out (output parameter of type json)
        ///     - result_out (output parameter of type boolean)
        /// </summary>
        /// <param name="dmRefreshLogId">Primary key of the data mart refresh log.</param>
        /// <param name="exception">The exception that captured the failure.</param>
        /// <returns><see langword="true"/> when the stored procedure returns a successful result; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown when <c>result_out</c> is missing or contains an unexpected value instead of a boolean success indicator.</exception>
        private bool CallDataMartRefreshes_LogFailed(int dmRefreshLogId, Exception exception)
        {
            const string storedProcedureFullName = "camddmw.dm_refresh_log_failed";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var dmRefreshLogIdInParameter = dbContext.CreateParameter("dmrefreshlogid_in", dmRefreshLogId.ToString(), NpgsqlDbType.Integer, System.Data.ParameterDirection.Input);
            var errorJsonInParameter = dbContext.CreateParameter("errorjson_in", exception?.ToString(), NpgsqlDbType.Json, System.Data.ParameterDirection.Input);
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Set the input parameter values, using DBNull.Value for nulls
            dmRefreshLogIdInParameter.Value = dmRefreshLogId;
            errorJsonInParameter.Value = (exception != null)
                                       ? JsonSerializer.Serialize
                                         (
                                           exception.GetType().ToString() == "Npgsql.PostgresException"
                                           ? new Dictionary<string, string>
                                             {
                                                { "ExceptionType", exception.GetType().ToString() },
                                                { "Message", exception.Message },
                                                { "StackTrace", exception.StackTrace ?? string.Empty },
                                                { "InnerException", exception.InnerException?.ToString() ?? string.Empty },
                                                { "Source", exception.Source ?? string.Empty },
                                                { "SqlState", ((PostgresException)exception).SqlState ?? string.Empty },
                                                { "Severity", ((PostgresException)exception).Severity ?? string.Empty },
                                                { "InvariantSeverity", ((PostgresException)exception).InvariantSeverity ?? string.Empty },
                                                { "MessageText", ((PostgresException)exception).MessageText ?? string.Empty },
                                                { "Detail", ((PostgresException)exception).Detail ?? string.Empty },
                                                { "Hint", ((PostgresException)exception).Hint ?? string.Empty },
                                                { "InternalQuery", ((PostgresException)exception).InternalQuery ?? string.Empty },
                                                { "Where", ((PostgresException)exception).Where ?? string.Empty },
                                                { "SchemaName", ((PostgresException)exception).SchemaName ?? string.Empty },
                                                { "TableName", ((PostgresException)exception).TableName ?? string.Empty },
                                                { "ColumnName", ((PostgresException)exception).ColumnName ?? string.Empty },
                                                { "DataTypeName", ((PostgresException)exception).DataTypeName ?? string.Empty },
                                                { "ConstraintName", ((PostgresException)exception).ConstraintName ?? string.Empty },
                                                { "File", ((PostgresException)exception).File ?? string.Empty },
                                                { "Line", ((PostgresException)exception).Line ?? string.Empty },
                                                { "Routine", ((PostgresException)exception).Routine ?? string.Empty },
                                                { "IsTransient", ((PostgresException)exception).IsTransient ? "True" : "False" }
                                             }
                                           : new Dictionary<string, string>
                                             {
                                                { "ExceptionType", exception.GetType().ToString() },
                                                { "Message", exception.Message },
                                                { "StackTrace", exception.StackTrace ?? string.Empty },
                                                { "InnerException", exception.InnerException?.ToString() ?? string.Empty },
                                                { "Source", exception.Source ?? string.Empty }
                                             }
                                         )
                                       : "{ \"Message\": \"No exception provided\" }";
            /*
            errorJsonInParameter.Value = (exception != null)
                                       ? JsonSerializer.Serialize
                                         (
                                            exception.GetType() // Retrieve the exact runtime Type of the exception.
                                                     .GetProperties() // Get all public properties of the exception type
                                                     .Where(p => p.Name != nameof(Exception.TargetSite)) // Exclude TargetSite property which returns MethodBase object that includes non-serializable data.
                                                     .ToDictionary(p => p.Name, p => p.GetValue(exception)?.ToString()) // Create a dictionary of property names and their string values.
                                         )
                                       : "{ \"Message\": \"No exception provided\" }";
            */

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { dmRefreshLogIdInParameter, errorJsonInParameter, errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool?)resultOutParameter.Value : null;


            logger.LogInformation("{StoredProcedure} completed with result: {Result}", storedProcedureFullName, Convert.ToString(result) ?? "null");

            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }

            return result ?? false;
        }


        /// <summary>
        /// Calls the camddmw.dm_refresh_log_start stored procedure.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - dmRefreshLogId (output parameter of type integer)
        ///     - errorjson_out (output parameter of type json)
        ///     - result_out (output parameter of type boolean)
        /// </summary>
        /// <param name="dmRefreshLogId">Primary key of the data mart refresh log.</param>
        /// <returns><see langword="true"/> when the stored procedure returns a successful result; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown when <c>result_out</c> is missing or contains an unexpected value instead of a boolean success indicator.</exception>
        private bool CallDataMartRefreshes_LogStart(out int? dmRefreshLogId)
        {
            const string storedProcedureFullName = "camddmw.dm_refresh_log_start";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var dmRefreshLogIdOutParameter = dbContext.CreateParameter("dmrefreshlogid_out", null, NpgsqlDbType.Integer, System.Data.ParameterDirection.Output);
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { dmRefreshLogIdOutParameter, errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            dmRefreshLogId = dmRefreshLogIdOutParameter.Value != DBNull.Value ? (int?)dmRefreshLogIdOutParameter.Value : null;
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool?)resultOutParameter.Value : null;


            logger.LogInformation("{StoredProcedure} completed with result: {Result}", storedProcedureFullName, Convert.ToString(result) ?? "null");

            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }
            else
            {
                logger.LogInformation("{StoredProcedure} indicates an incremental refresh is required.", storedProcedureFullName);
            }

            return result ?? false;
        }


        /// <summary>
        /// Calls camddmw.dm_refresh_update_supporting_data stored procedure and returns the result of the stored procedure.  If the stored procedure returns false, then an error occurred and the error details are logged.
        /// 
        /// The stored procedure must have the following parameters:
        /// 
        ///     - errorjson_out (json, output)
        ///     - result_out (boolean, output)
        /// </summary>
        /// <returns><see langword="true"/> when the stored procedure returns a successful result; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown when <c>result_out</c> is missing or contains an unexpected value instead of a boolean success indicator.</exception>
        private bool CallDataMartRefreshes_UpdateSupportingData()
        {
            const string storedProcedureFullName = "camddmw.dm_refresh_update_supporting_data";

            logger.LogInformation("{StoredProcedure} starting", storedProcedureFullName);

            // Create parameters for the stored procedure
            var errorJsonOutParameter = dbContext.CreateParameter("errorjson_out", null, NpgsqlDbType.Json, System.Data.ParameterDirection.Output);
            var resultOutParameter = dbContext.CreateParameter("result_out", null, NpgsqlDbType.Boolean, System.Data.ParameterDirection.Output);

            // Execute the stored procedure
            dbContext.ExecuteProcedure(
                storedProcedureFullName,
                new () { errorJsonOutParameter, resultOutParameter }
            );

            // Get the output values
            var errorJson = errorJsonOutParameter.Value != DBNull.Value ? errorJsonOutParameter.Value.ToString() : null;
            var result = resultOutParameter.Value != DBNull.Value ? (bool)resultOutParameter.Value : (bool?)null;


            // Log the results
            if (result == null)
            {
                // Unexpected result value - treat as error
                logger.LogError("{StoredProcedure} returned unexpected Result value: {Result}. Expected 'True' or 'False'. Error details: {ErrorJson}.",
                                 storedProcedureFullName, Convert.ToString(result) ?? "null", errorJson ?? "null");
                throw new Exception($"{storedProcedureFullName} returned unexpected Result value: '{Convert.ToString(result) ?? "null"}'. Expected 'True' or 'False'.");
            }
            else if (!result.Value)
            {
                if (!string.IsNullOrWhiteSpace(errorJson))
                    logger.LogError("{StoredProcedure} returned an error. Error details: {ErrorJson}.",
                                     storedProcedureFullName, errorJson ?? "null");
                else
                    logger.LogError("{StoredProcedure} returned an error.",
                                     storedProcedureFullName);
            }
            else
            {
                logger.LogInformation("{StoredProcedure} completed successfully", storedProcedureFullName);
            }

            return result ?? false;
        }


        #region Public Static Methods

        /// <summary>
        /// Registers the DataMartRefreshJob with Quartz using the configured job key and description.
        /// </summary>
        /// <param name="services">The service collection used to register Quartz job configuration.</param>
        public static void RegisterWithQuartz(IServiceCollection services)
        {
            services.AddQuartzJob<DataMartRefreshJob>(WithJobKey(), JobDescription);
        }


        /// <summary>
        /// Creates a <see cref="JobKey"/> using the configured job name and job group.
        /// </summary>
        /// <returns>A <see cref="JobKey"/> initialized with <c>JobName</c> and <c>JobGroup</c>.</returns>
        private static JobKey WithJobKey()
        {
            return new JobKey(JobName, JobGroup);
        }

        #endregion


        #region Helper Private Methods
        
        /// <summary>
        /// Determines whether the job is currently in progress based on the last job log entry.
        /// </summary>
        /// <returns><see langword="true"/> if the last job log status is "WIP"; otherwise, <see langword="false"/>.</returns>
        private bool IsJobInProgress()
        {
            // Get the last job log record for the job name
            JobLog lastJobLog = dbContext.JobLogs.FromSqlRaw(@"
                        SELECT * FROM camdaux.job_log
                        WHERE job_name = '{0}'
                        ORDER BY start_date DESC
                        LIMIT 1", JobName
                    )?.FirstOrDefault();

            // Check the status of the last job log. If the status is "WIP", then return true.
            if (lastJobLog != null && lastJobLog.StatusCd == "WIP")
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Creates a new job log entry with generated job metadata, start time in Eastern Time, and an in-progress
        /// status, then persists it to the database.
        /// </summary>
        /// <remarks>Initial values include a new job identifier, "Quartz" as the job system, the current
        /// class name, the configured job name, the current Eastern Time start date, and "WIP" as the status
        /// code.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created and persisted job
        /// log entry.</returns>
        private async Task<JobLog> JobLogCreate()
        {
            JobLog jobLog = new ()
            {
                JobId = Guid.NewGuid(),
                JobSystem = "Quartz",
                JobClass = this.GetType().Name,
                JobName = JobName,
                StatusCd = "WIP",
                StartDate = Utils.getCurrentEasternTime(),
                EndDate = null,
                AddDate = Utils.getCurrentEasternTime()
            };

            dbContext.JobLogs.Add(jobLog);
            await dbContext.SaveChangesAsync();

            return jobLog;
        }


        /// <summary>
        /// Marks the specified job log as failed, sets its end time, stores additional details, and persists the
        /// update.
        /// </summary>
        /// <remarks>Sets <c>StatusCd</c> to <c>"ERROR"</c> and <c>EndDate</c> to the current Eastern Time
        /// before saving changes.</remarks>
        /// <param name="jobLog">The job log entry to update.</param>
        /// <param name="additionalDetails">Additional information describing the failure.</param>
        /// <returns>A task that represents the asynchronous update and save operation.</returns>
        private async Task JobLogMarkFailed(JobLog jobLog, string additionalDetails)
        {
            // Mark job as completed
            jobLog.StatusCd = "ERROR";
            jobLog.EndDate = Utils.getCurrentEasternTime();
            jobLog.AdditionalDetails = additionalDetails;
            dbContext.JobLogs.Update(jobLog);
            await dbContext.SaveChangesAsync();
        }


        /// <summary>
        /// Marks a job log as completed, sets the end time to the current Eastern time, updates the record, and
        /// persists the change asynchronously.
        /// </summary>
        /// <param name="jobLog">The job log entry to mark as successful.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        private async Task JobLogMarkSuccessful(JobLog jobLog)
        {

            // Mark job as completed
            jobLog.StatusCd = "COMPLETE";
            jobLog.EndDate = Utils.getCurrentEasternTime();
            dbContext.JobLogs.Update(jobLog);
            await dbContext.SaveChangesAsync();
        }

        #endregion

    }

}
