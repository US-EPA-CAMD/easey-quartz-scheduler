﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.CheckEngine.SpecialParameterClasses;
using ECMPS.Checks.Data.Ecmps.CheckEm.Function;
using ECMPS.Checks.Em.Parameters;
using ECMPS.Checks.EmissionsReport;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

using ECMPS.Definitions.Extensions;


namespace ECMPS.Checks.EmissionsChecks
{

    public class WeeklySystemIntegrityChecks : cEmissionsChecks
    {

        #region Constructors

        public WeeklySystemIntegrityChecks(cEmissionsReportProcess emissionReportProcess)
            : base(emissionReportProcess)
        {
            CheckProcedures = new dCheckProcedure[12];

            CheckProcedures[1] = new dCheckProcedure(EMWSI1);
            CheckProcedures[2] = new dCheckProcedure(EMWSI2);
            CheckProcedures[3] = new dCheckProcedure(EMWSI3);
            CheckProcedures[4] = new dCheckProcedure(EMWSI4);
            CheckProcedures[5] = new dCheckProcedure(EMWSI5);
            CheckProcedures[6] = new dCheckProcedure(EMWSI6);
            CheckProcedures[7] = new dCheckProcedure(EMWSI7);
            CheckProcedures[8] = new dCheckProcedure(EMWSI8);
            CheckProcedures[9] = new dCheckProcedure(EMWSI9);
            CheckProcedures[10] = new dCheckProcedure(EMWSI10);
            CheckProcedures[11] = new dCheckProcedure(EMWSI11);
        }

        #endregion


        #region Check Methods 1 - 10

        /// <summary>
        /// Check Hg Converter Indicator of the Component
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI1(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CurrentWeeklySystemIntegrityTest.HgConverterInd != 1)
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "A";
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        /// <summary>
        /// Check Gas Level
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI2(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CurrentWeeklySystemIntegrityTest.GasLevelCd == null)
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "A";
                }
                else if (emParams.CurrentWeeklySystemIntegrityTest.GasLevelCd.NotInList("HIGH,MID,LOW,ZERO"))
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "B";
                }
                else if (emParams.CurrentWeeklySystemIntegrityTest.GasLevelCd.NotInList("HIGH,MID"))
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "C";
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// Check Weekly System Integrity Reference Value
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI3(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.InjectionReferenceValueValid = false;

                if (emParams.CurrentWeeklySystemIntegrityTest.RefValue == null)
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "A";
                }
                else if (!emParams.CurrentWeeklySystemIntegrityTest.RefValue.Value.IsRounded(1))
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "B";
                }
                else if (emParams.CurrentWeeklySystemIntegrityTest.RefValue.Value <= 0)
                {
                    if (emParams.CurrentWeeklySystemIntegrityTest.TestResultCd != "FAILED")
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "C";
                    }
                }
                else
                {
                    emParams.InjectionReferenceValueValid = true;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// Check Weekly System Integrity Measured Value
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI4(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.InjectionMeasuredValueValid = false;

                if (emParams.CurrentWeeklySystemIntegrityTest.MeasuredValue == null)
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "A";
                }
                else if (!emParams.CurrentWeeklySystemIntegrityTest.MeasuredValue.Value.IsRounded(1))
                {
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "B";
                }
                else if (emParams.CurrentWeeklySystemIntegrityTest.MeasuredValue.Value <= 0)
                {
                    if (emParams.CurrentWeeklySystemIntegrityTest.TestResultCd != "FAILED")
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "C";
                    }
                }
                else
                {
                    emParams.InjectionMeasuredValueValid = true;
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// Calculate System Integrity Error and Alternate Performance Spec Indicator
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI5(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.CalculatedSystemIntegrityApsIndicator = null;
                emParams.CalculatedSystemIntegrityError = null;

                if (emParams.InjectionReferenceValueValid.Default(false) && emParams.InjectionMeasuredValueValid.Default(false))
                {
                    decimal percentError = 100
                                         * Math.Abs(emParams.CurrentWeeklySystemIntegrityTest.RefValue.Value - emParams.CurrentWeeklySystemIntegrityTest.MeasuredValue.Value)
                                         / emParams.CurrentWeeklySystemIntegrityTest.RefValue.Value;
                    {
                        percentError = Math.Round(percentError, 1, MidpointRounding.AwayFromZero);
                    }

                    if (percentError <= 10)
                    {
                        emParams.CalculatedSystemIntegrityApsIndicator = 0;
                        emParams.CalculatedSystemIntegrityError = percentError;
                        emParams.CalculatedWeeklyTestSummaryResult = "PASSED";
                    }
                    else
                    {
                        decimal absoluteError = Math.Abs(emParams.CurrentWeeklySystemIntegrityTest.RefValue.Value - emParams.CurrentWeeklySystemIntegrityTest.MeasuredValue.Value);
                        {
                            absoluteError = Math.Round(absoluteError, 1, MidpointRounding.AwayFromZero);
                        }

                        if (absoluteError <= 0.8m)
                        {
                            emParams.CalculatedSystemIntegrityApsIndicator = 1;
                            emParams.CalculatedSystemIntegrityError = absoluteError;
                            emParams.CalculatedWeeklyTestSummaryResult = "PASSAPS";
                        }
                        else
                        {
                            emParams.CalculatedSystemIntegrityApsIndicator = 0;
                            emParams.CalculatedSystemIntegrityError = percentError;
                            emParams.CalculatedWeeklyTestSummaryResult = "FAILED";
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
        /// Check Weekly System Integrity Alternative Performance Spec
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI6(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.WeeklySystemIntegrityApsIsValid = false;

                if (emParams.InjectionReferenceValueValid.Default(false) && emParams.InjectionMeasuredValueValid.Default(false))
                {
                    if (emParams.CurrentWeeklySystemIntegrityTest.ApsInd == null)
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "A";
                    }
                    else if ((emParams.CurrentWeeklySystemIntegrityTest.ApsInd != 0) && (emParams.CurrentWeeklySystemIntegrityTest.ApsInd != 1))
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "B";
                    }
                    else if (emParams.CurrentWeeklySystemIntegrityTest.ApsInd != emParams.CalculatedSystemIntegrityApsIndicator)
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "C";
                    }
                    else
                    {
                        emParams.WeeklySystemIntegrityApsIsValid = true;
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
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI7(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                emParams.WeeklySystemIntegrityErrorIsValid = false;

                if (emParams.InjectionReferenceValueValid.Default(false) && emParams.InjectionMeasuredValueValid.Default(false))
                {
                    if (emParams.CurrentWeeklySystemIntegrityTest.SystemIntegrityError == null)
                    {
                        emParams.WeeklyTestSummaryValid = false;
                        category.CheckCatalogResult = "A";
                    }
                    else if (emParams.WeeklySystemIntegrityApsIsValid.Default(false))
                    {
                        if (!emParams.CurrentWeeklySystemIntegrityTest.SystemIntegrityError.Value.IsRounded(1))
                        {
                            emParams.WeeklyTestSummaryValid = false;
                            category.CheckCatalogResult = "B";
                        }
                        else if (emParams.CurrentWeeklySystemIntegrityTest.SystemIntegrityError != emParams.CalculatedSystemIntegrityError)
                        {
                            emParams.WeeklyTestSummaryValid = false;
                            category.CheckCatalogResult = "C";
                        }
                        else
                        {
                            emParams.WeeklySystemIntegrityErrorIsValid = true;
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
        /// Check Weekly Test Summary Result Against Calculated Value
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI8(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if ((emParams.CalculatedWeeklyTestSummaryResult != null) &&
                    (emParams.CurrentWeeklySystemIntegrityTest.TestResultCd != emParams.CalculatedWeeklyTestSummaryResult))
                {
                    emParams.CalculatedWeeklyTestSummaryResult = null;
                    emParams.WeeklyTestSummaryValid = false;
                    category.CheckCatalogResult = "A";
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// Check Weekly Test Summary Result Against Calculated Value
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI9(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CurrentWeeklySystemIntegrityTest.ComponentId != null)
                {
                    if (emParams.WsiTestDictionary.Count == 0 ||
                    (emParams.WsiTestDictionary.Count > 0 && !emParams.WsiTestDictionary.ContainsKey(emParams.CurrentWeeklySystemIntegrityTest.ComponentId)))
                    {
                        emParams.WsiTestDictionary[emParams.CurrentWeeklySystemIntegrityTest.ComponentId] = new WsiTestStatusInformation();
                    }

                    emParams.WsiTestDictionary[emParams.CurrentWeeklySystemIntegrityTest.ComponentId].MostRecentTestRecord = emParams.CurrentWeeklySystemIntegrityTest;
                }
            }

            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }


        /// <summary>
        /// Update Weekly System Integrity Dictionary Operating date Information.
        /// 
        /// 1) Clear the Operating Date List when the last evaluated test becomes the most recent test.
        /// 2) Only add the curent operating date to Operating Date List if the date is after the most recent test.
        ///    Operating after the test on the day of the test does not count as an operating day.
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI10(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CurrentOperatingTime.Default(0) > 0)
                {
                    foreach (WsiTestStatusInformation wsiTestEntry in emParams.WsiTestDictionary.Values)
                    {
                        if ((wsiTestEntry.MostRecentTestRecord != null) && (wsiTestEntry.MostRecentTestRecord.MonLocId == emParams.CurrentMonitorLocationId))
                        {
                            //reset the opDates on the first hour
                            if (wsiTestEntry.MostRecentTestRecord.TestDatehour == emParams.CurrentDateHour)
                            {
                                wsiTestEntry.OperatingDateList.Clear();
                            }

                            //add the current OpDate if it's not already there
                            if ((wsiTestEntry.MostRecentTestRecord.TestDate < emParams.CurrentOperatingDate.Value) &&
                                (!wsiTestEntry.OperatingDateList.Contains(emParams.CurrentOperatingDate.Value)))
                            {
                                wsiTestEntry.OperatingDateList.Add(emParams.CurrentOperatingDate.Value);
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


        #region Check Methods 11 - 20

        /// <summary>
        /// Ensure that Weekly System Integrity Test Occurred During an Operating Hour
        /// </summary>
        /// <param name="category">Category Object</param>
        /// <param name="log">Indicates whether to log results.</param>
        /// <returns>Returns error message if check fails to run correctly.</returns>
        public  string EMWSI11(cCategory category, ref bool log)
        {
            string returnVal = "";

            try
            {
                if (emParams.CurrentOperatingTime == 0)
                {
                    category.CheckCatalogResult = "A";
                }
            }
            catch (Exception ex)
            {
                returnVal = category.CheckEngine.FormatError(ex);
            }

            return returnVal;
        }

        #endregion

    }

}
