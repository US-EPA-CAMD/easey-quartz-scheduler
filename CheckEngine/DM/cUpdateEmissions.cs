using System;
using System.Data;

using ECMPS.DM.Definitions;
using ECMPS.DM.HourlyEmissions;
using ECMPS.DM.Miscellaneous;
using ECMPS.DM.Utilities;

using ECMPS.Checks.DatabaseAccess;
using ECMPS.Definitions.Extensions;
using ECMPS.Checks.EmissionsReport;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Runtime;


namespace ECMPS.DM
{

    /// <summary>
    /// Used to update DataMart (AMPD) Emissions for an Emission Report
    /// </summary>
    public class cUpdateEmissions
    {

        #region Public Constructors

        /// <summary>
        /// Creates a cUpdateEmissions object.
        /// </summary>
        /// <param name="dbConnectionString">The database string for the ECMPS PostgreSQL database.</param>
        /// <param name="logger">The ILogger instance to use.</param>
        /// <param name="commandTimeout">The timeout to use</param>
        public cUpdateEmissions( string dbConnectionString, ILogger<cUpdateEmissionsDb> logger, int commandTimeout = 300 ) 
        {
            cDatabase.AuxConnectionString = dbConnectionString;
            cDatabase.DataConnectionString = dbConnectionString;
            cDatabase.WorkspaceConnectionString = dbConnectionString;

            _dbConnection = cDatabase.GetConnection( cDatabase.eCatalog.AUX, commandTimeout, "Apportionment" );
            _logger = logger;
            _updateEmissionsDb = new cUpdateEmissionsDb( _dbConnection, logger);
        }
        
        #endregion


        #region Public Properties

        /// <summary>
        /// The MON_PLAN_ID of the emissions report being processed
        /// </summary>
        public string MonPlanId { get; set; }

        /// <summary>
        /// The RPT_PERIOD_ID of the emissions report being processed
        /// </summary>
        public int RptPeriodId { get; set; }

        #endregion


        #region Private Fields

        private readonly cDatabase _dbConnection;
        private readonly ILogger<cUpdateEmissionsDb> _logger;
        private readonly cUpdateEmissionsDb _updateEmissionsDb;


        #endregion


        #region Public Methods

        /// <summary>
        /// Performs the actions needed to create the DataMart (AMPD) Emissions data.
        /// </summary>
        /// <param name="pdemReportId">The PDEM_REPORT_ID of the update.</param>
        public void ProcessEmissionReport(long pdemReportId)
        {
            try
            {
                _updateEmissionsDb.TransactionBegin();

                string errorMessage = "";

                string monPlanId;
                int? rptPeriodId;
                bool? isMatsEmissionReport;
                DataTable locationTable;
                DataTable rptPeriodInfoTable;
                DataTable locationTypeCountTable;
                DataTable locationLinkSpanCountTable;
                DataTable locationLinkActiveTable;
                DataTable specialMethodCountTable;
                DataTable monitorHourTable;

                if (_updateEmissionsDb.UpdateInit(pdemReportId,
                                                  out monPlanId, 
                                                  out rptPeriodId,
                                                  out isMatsEmissionReport,
                                                  out locationTable,
                                                  out rptPeriodInfoTable,
                                                  out locationTypeCountTable,
                                                  out locationLinkSpanCountTable,
                                                  out locationLinkActiveTable,
                                                  out specialMethodCountTable,
                                                  out monitorHourTable,
                                                  ref errorMessage))
                {
                    if (rptPeriodInfoTable.Rows.Count == 1)
                    {
                        int year = rptPeriodInfoTable.Rows[0]["YEAR"].AsInteger().Default();
                        int quarter = rptPeriodInfoTable.Rows[0]["QUARTER"].AsInteger().Default();

                        eApportionmentType apportionmentType = DetermineApportionmentType(locationTypeCountTable,
                                                                                          locationLinkSpanCountTable,
                                                                                          locationLinkActiveTable,
                                                                                          specialMethodCountTable);

                        switch (apportionmentType)
                        {
                            case eApportionmentType.Error:
                                {
                                    _updateEmissionsDb.TransactionRollback();
                                    errorMessage = "Unable to determine apportionment type.";
                                    _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                                    _updateEmissionsDb.UpdateFailure(pdemReportId, apportionmentType, errorMessage);
                                }
                                break;

                            case eApportionmentType.MultiplePipe:
                                {
                                    _updateEmissionsDb.TransactionRollback();
                                    errorMessage = "Multiple pipe apportionment not supported";
                                    _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                                    _updateEmissionsDb.UpdateFailure(pdemReportId, apportionmentType, errorMessage);
                                }
                                break;

                            case eApportionmentType.MultiplePipeInvolved:
                                {
                                    _updateEmissionsDb.TransactionRollback();
                                    errorMessage = "Apportionment involving multiple pipes not supported";
                                    _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                                    _updateEmissionsDb.UpdateFailure(pdemReportId, apportionmentType, errorMessage);
                                }
                                break;

                            default:
                                {
                                    cLocationInfo[] locationInfo;

                                    if (GetLocationInfo(locationTable, out locationInfo))
                                    {
                                        cHourlyRawData hourlyRawData = new cHourlyRawData(pdemReportId, monPlanId, rptPeriodId.Value, locationInfo, year, quarter, _logger);

                                        cFactorFormulae[] factorFormulaeArray;
                                        cHourlyApportionedData hourlyApportionedData;

                                        if (hourlyRawData.Update(monitorHourTable) &&
                                            _updateEmissionsDb.GetFactorFormulaeArray(monPlanId, rptPeriodId.Value, hourlyRawData.UnitInfo, hourlyRawData.LocationInfo, out factorFormulaeArray, out errorMessage) &&
                                            GetApportionedData(apportionmentType, hourlyRawData, factorFormulaeArray, out hourlyApportionedData) &&
                                            _updateEmissionsDb.UpdateUnitHourData(isMatsEmissionReport, hourlyApportionedData, out errorMessage) &&
                                            _updateEmissionsDb.UpdatePublic(pdemReportId, out errorMessage) &&
                                            _updateEmissionsDb.UpdateSuccess(pdemReportId, apportionmentType, isMatsEmissionReport, out errorMessage))
                                        {
                                            _updateEmissionsDb.TransactionCommit();
                                        }
                                        else
                                        {
                                            _updateEmissionsDb.TransactionRollback();
                                            _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                                            _updateEmissionsDb.UpdateFailure(pdemReportId, apportionmentType, errorMessage);
                                        }
                                    }
                                    else
                                    {
                                        _updateEmissionsDb.TransactionRollback();
                                        _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                                        _updateEmissionsDb.UpdateFailure(pdemReportId, apportionmentType, errorMessage);
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        _updateEmissionsDb.TransactionRollback();
                        errorMessage = $"Unknown reporting period: {rptPeriodId}";
                        _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                        _updateEmissionsDb.UpdateFailure(pdemReportId, null, errorMessage);
                    }
                }
                else
                {
                    _updateEmissionsDb.TransactionRollback();
                    _logger?.LogError("PDEM.ProcessEmissionReport({pdemReportId}): {errorMessage}", pdemReportId, errorMessage);
                    _updateEmissionsDb.UpdateFailure(pdemReportId, null, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _updateEmissionsDb.TransactionRollback();
                _logger?.LogError(ex, "PDEM.ProcessEmissionReport({pdemReportId})", pdemReportId);
                _updateEmissionsDb.UpdateFailure(pdemReportId, null, ex.Message);
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Returns an apportionment class corresponding to the indicated apportionment type.
        /// </summary>
        /// <param name="apportionmentType">The apportionment type of the apportionment class.</param>
        /// <param name="hourlyRawData">The raw data to apportion.</param>
        /// <param name="factorFormulaeArray"></param>
        /// <param name="hourlyApportionedData">The resulting apportionment class.</param>
        /// <returns>False if the apportionment type is not handled or an unhandled exception occurs.</returns>
        public bool GetApportionedData(eApportionmentType apportionmentType,
                                       cHourlyRawData hourlyRawData,
                                       cFactorFormulae[] factorFormulaeArray,
                                       out cHourlyApportionedData hourlyApportionedData)
        {
            bool result;

            try
            {
                if ((factorFormulaeArray != null) && (factorFormulaeArray.Length > 0))
                {
                    hourlyApportionedData = new cHourlyApportionedDataComplex(hourlyRawData, factorFormulaeArray, _logger);
                }
                else
                {
                    switch (apportionmentType)
                    {
                        case eApportionmentType.CommonPipe:
                            hourlyApportionedData = new cHourlyApportionedDataCommonPipe(hourlyRawData, _logger);
                            break;

                        case eApportionmentType.CommonPipeLtff:
                            hourlyApportionedData = new cHourlyApportionedDataUnit(hourlyRawData, _logger);
                            break;

                        case eApportionmentType.CommonStack:
                            hourlyApportionedData = new cHourlyApportionedDataCommonStack(hourlyRawData, _logger);
                            break;

                        case eApportionmentType.CommonStackAndPipe:
                            hourlyApportionedData = new cHourlyApportionedDataCommonStackAndPipe(hourlyRawData, _logger);
                            break;

                        case eApportionmentType.MultipleStack:
                            hourlyApportionedData = new cHourlyApportionedDataMultipleStack(hourlyRawData, _logger);
                            break;

                        case eApportionmentType.Unit:
                            hourlyApportionedData = new cHourlyApportionedDataUnit(hourlyRawData, _logger);
                            break;

                        default:
                            hourlyApportionedData = null;
                            break;
                    }
                }

                if (hourlyApportionedData != null)
                {
                    result = hourlyApportionedData.Apportion();
                }
                else
                {
                    _logger?.LogError("PDEM.GetApportionedData({apportionmentType}): Apportionment Type not handled.", apportionmentType);
                    result = false;
                }
            }
            catch (Exception ex)
            {
                hourlyApportionedData = null;
                _logger?.LogError(ex, "PDEM.GetApportionedData({apportionmentType})", apportionmentType);
                result = false;
            }

            return result;
        }


        private eApportionmentType DetermineApportionmentType(DataTable locationTypeCountTable,
                                                              DataTable locationLinkSpanCountTable,
                                                              DataTable locationLinkActiveTable,
                                                              DataTable specialMethodCountTable)
        {
            eApportionmentType result;

            try
            {

                if ((locationTypeCountTable.Rows.Count == 1) &&
                    (locationLinkSpanCountTable.Rows.Count <= 1) &&
                    (locationLinkActiveTable.Rows.Count <= 1) &&
                    (specialMethodCountTable.Rows.Count == 1))
                {
                    // Location Type Counts
                    int csCount = locationTypeCountTable.Rows[0]["CS"].AsInteger().Default(0);
                    int msCount = locationTypeCountTable.Rows[0]["MS"].AsInteger().Default(0);
                    int cpCount = locationTypeCountTable.Rows[0]["CP"].AsInteger().Default(0);
                    int mpCount = locationTypeCountTable.Rows[0]["MP"].AsInteger().Default(0);
                    int unCount = locationTypeCountTable.Rows[0]["UN"].AsInteger().Default(0);

                    // Location Link Span Counts
                    int csLinkSpanCount = ((locationLinkSpanCountTable.Rows.Count == 1)
                                        ? locationLinkSpanCountTable.Rows[0]["CS"].AsInteger().Default(0)
                                        : 0);
                    int msLinkSpanCount = ((locationLinkSpanCountTable.Rows.Count == 1)
                                        ? locationLinkSpanCountTable.Rows[0]["MS"].AsInteger().Default(0)
                                        : 0);
                    int cpLinkSpanCount = ((locationLinkSpanCountTable.Rows.Count == 1)
                                        ? locationLinkSpanCountTable.Rows[0]["CP"].AsInteger().Default(0)
                                        : 0);
                    int mpLinkSpanCount = ((locationLinkSpanCountTable.Rows.Count == 1)
                                        ? locationLinkSpanCountTable.Rows[0]["MP"].AsInteger().Default(0)
                                        : 0);

                    // Location Link Active Counts
                    int csLinkActiveCount = ((locationLinkActiveTable.Rows.Count == 1)
                                          ? locationLinkActiveTable.Rows[0]["CS"].AsInteger().Default(0)
                                          : 0);
                    int msLinkActiveCount = ((locationLinkActiveTable.Rows.Count == 1)
                                          ? locationLinkActiveTable.Rows[0]["MS"].AsInteger().Default(0)
                                          : 0);
                    int cpLinkActiveCount = ((locationLinkActiveTable.Rows.Count == 1)
                                          ? locationLinkActiveTable.Rows[0]["CP"].AsInteger().Default(0)
                                          : 0);
                    int mpLinkActiveCount = ((locationLinkActiveTable.Rows.Count == 1)
                                          ? locationLinkActiveTable.Rows[0]["MP"].AsInteger().Default(0)
                                          : 0);

                    // CP LTFF Active
                    bool cpLtffActive = (specialMethodCountTable.Rows[0]["CP_LTFF"].AsInteger().Default(0) >= 1);

                    if ((unCount >= 1) && (csCount == 0) && (msCount == 0) && (cpCount == 0) && (mpCount == 0))
                    {
                        result = eApportionmentType.Unit;
                    }
                    // Always peform after sunit test
                    else if ((csLinkActiveCount == 0) && (msLinkActiveCount == 0) &&
                             (cpLinkActiveCount == 0) && (mpLinkActiveCount == 0))
                    {
                        result = eApportionmentType.Error;
                    }
                    // Always perform after error test and before Changed and Complex test.
                    else if (mpCount > 0)
                    {
                        if ((unCount == 1) && (csCount == 0) && (msCount == 0) && (cpCount == 0) && (mpCount > 1))
                            result = eApportionmentType.MultiplePipe;
                        else
                            result = eApportionmentType.MultiplePipeInvolved;
                    }
                    else if ((csLinkSpanCount != csLinkActiveCount) || (msLinkSpanCount != msLinkActiveCount) ||
                             (cpLinkSpanCount != cpLinkActiveCount) || (mpLinkSpanCount != mpLinkActiveCount))
                    {
                        result = eApportionmentType.Changed;
                    }
                    else if (((csCount * unCount) != csLinkSpanCount) ||
                             ((msCount * unCount) != msLinkSpanCount) ||
                             ((cpCount * unCount) != cpLinkSpanCount) ||
                             ((mpCount * unCount) != mpLinkSpanCount))
                    {
                        result = eApportionmentType.Complex;
                    }
                    else if ((unCount > 1) && (csCount >= 1) && (msCount == 0) && (cpCount == 0) && (mpCount == 0))
                    {
                        result = eApportionmentType.CommonStack;
                    }
                    else if ((unCount > 1) && (csCount == 0) && (msCount == 0) && (cpCount >= 1) && (mpCount == 0))
                    {
                        if (cpLtffActive)
                            result = eApportionmentType.CommonPipeLtff;
                        else
                            result = eApportionmentType.CommonPipe;
                    }
                    else if ((unCount == 1) && (csCount == 0) && (msCount > 1) && (cpCount == 0) && (mpCount == 0))
                    {
                        result = eApportionmentType.MultipleStack;
                    }
                    else if ((unCount > 1) && (csCount >= 1) && (msCount == 0) && (cpCount >= 1) && (mpCount == 0))
                    {
                        result = eApportionmentType.CommonStackAndPipe;
                    }
                    else
                    {
                        result = eApportionmentType.Complex;
                    }
                }
                else
                {
                    throw new Exception("On or more Location Information tables have unexpected row counts.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDEM.DetermineApportionmentType()");
                result = eApportionmentType.Error;
            }

            return result;
        }

        private bool GetLocationInfo(DataTable locationTable, out cLocationInfo[] locationInfo)
        {
            bool result = true;

            locationInfo = new cLocationInfo[locationTable.Rows.Count];

            for (int locationDex = 0;
                 result && locationDex < locationTable.Rows.Count;
                 locationDex++)
            {
                DataRow locationRow = locationTable.Rows[locationDex];

                try
                {
                    locationInfo[locationDex] = new cLocationInfo(locationRow["MON_LOC_ID"].AsString(),
                                                                  locationRow["UNIT_ID"].AsInteger(),
                                                                  locationRow["LOCATION_NAME"].AsString());
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"GetLocationInfoMON_LOC_ID: {locationRow["MON_LOC_ID"]}");
                    result = false;
                }
            }

            return result;
        }

        #endregion

    }

}
