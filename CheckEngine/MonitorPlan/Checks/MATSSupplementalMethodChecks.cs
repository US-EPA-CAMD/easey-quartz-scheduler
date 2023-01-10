using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

using ECMPS.Checks.Mp.Parameters;

namespace ECMPS.Checks.MATSSupplementalMethodChecks
{
	public class cMATSSupplementalMethodChecks : cChecks
	{

		public cMATSSupplementalMethodChecks()
		{
			CheckProcedures = new dCheckProcedure[12];

			CheckProcedures[1] = new dCheckProcedure(MATSMTH1);
			CheckProcedures[2] = new dCheckProcedure(MATSMTH2);
			CheckProcedures[3] = new dCheckProcedure(MATSMTH3);
			CheckProcedures[4] = new dCheckProcedure(MATSMTH4);
			CheckProcedures[5] = new dCheckProcedure(MATSMTH5);
			CheckProcedures[6] = new dCheckProcedure(MATSMTH6);
			CheckProcedures[7] = new dCheckProcedure(MATSMTH7);
			CheckProcedures[8] = new dCheckProcedure(MATSMTH8);
			CheckProcedures[9] = new dCheckProcedure(MATSMTH9);
			CheckProcedures[10] = new dCheckProcedure(MATSMTH10);
			CheckProcedures[11] = new dCheckProcedure(MATSMTH11);
		}


		#region Check 1 thru 10

		/// <summary>
		/// MATSMTH-1: Begin Date
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH1(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentBeginDateValid = false;

				if (mpParams.MatsSupplementalComplianceMethodRecord.BeginDate == null)
				{
					Category.CheckCatalogResult = "A";
				}
				else if ((mpParams.MatsEvaluationBeginDate != null) && (mpParams.MatsSupplementalComplianceMethodRecord.BeginDate < mpParams.MatsEvaluationBeginDate))
				{
					Category.CheckCatalogResult = "B";
				}
				else if ((mpParams.MaximumFutureDate != null) && (mpParams.MatsSupplementalComplianceMethodRecord.BeginDate > mpParams.MaximumFutureDate))
				{
					Category.CheckCatalogResult = "C";
				}
				else
					mpParams.CurrentBeginDateValid = true;
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH1");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-2: Begin Hour
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH2(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentBeginDateAndHour = null;
				mpParams.CurrentBeginDateAndHourValid = false;

				if (mpParams.MatsSupplementalComplianceMethodRecord.BeginHour == null)
				{
					Category.CheckCatalogResult = "A";
				}
				else if (mpParams.MatsSupplementalComplianceMethodRecord.BeginHour < 0 || mpParams.MatsSupplementalComplianceMethodRecord.BeginHour > 23)
				{
					Category.CheckCatalogResult = "B";
				}
				else if (mpParams.CurrentBeginDateValid.Default(false))
				{
					mpParams.CurrentBeginDateAndHour = mpParams.MatsSupplementalComplianceMethodRecord.BeginDate.Value.AddHours((int)mpParams.MatsSupplementalComplianceMethodRecord.BeginHour.Value);
					mpParams.CurrentBeginDateAndHourValid = true;
				}

			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH2");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-3: End Date
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH3(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentEndDateValid = false;

				if (mpParams.MatsSupplementalComplianceMethodRecord.EndDate != null)
					if ((mpParams.MatsEvaluationBeginDate != null) && (mpParams.MatsSupplementalComplianceMethodRecord.EndDate < mpParams.MatsEvaluationBeginDate))
					{
						Category.CheckCatalogResult = "A";
					}
					else if ((mpParams.MaximumFutureDate != null) && (mpParams.MatsSupplementalComplianceMethodRecord.EndDate > mpParams.MaximumFutureDate))
					{
						Category.CheckCatalogResult = "B";
					}
					else
					{
						mpParams.CurrentEndDateValid = true;
					}
				else
				{
					mpParams.CurrentEndDateValid = true;
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH3");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-4: End Hour
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH4(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentEndDateAndHour = null;
				mpParams.CurrentEndDateAndHourValid = false;

				if (mpParams.MatsSupplementalComplianceMethodRecord.EndHour != null)
				{
					if (mpParams.MatsSupplementalComplianceMethodRecord.EndDate == null)
					{
						Category.CheckCatalogResult = "A";
					}
					else if (mpParams.MatsSupplementalComplianceMethodRecord.EndHour < 0 || mpParams.MatsSupplementalComplianceMethodRecord.EndHour > 23)
					{
						Category.CheckCatalogResult = "B";
					}
					else if (mpParams.CurrentEndDateValid.Default(false))
					{
						mpParams.CurrentEndDateAndHour = mpParams.MatsSupplementalComplianceMethodRecord.EndDate.Value.AddHours((int)mpParams.MatsSupplementalComplianceMethodRecord.EndHour.Value);
						mpParams.CurrentEndDateAndHourValid = true;
					}
				}
				else
					if (mpParams.MatsSupplementalComplianceMethodRecord.EndDate != null)
					{
						Category.CheckCatalogResult = "C";
					}
					else if (mpParams.CurrentEndDateValid.Default(false))
					{
						mpParams.CurrentEndDateAndHour = null;
						mpParams.CurrentEndDateAndHourValid = true;
					}


			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH4");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-5: Consistent Dates and Hours
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH5(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentDatesAndHoursConsistent = false;
				if (mpParams.CurrentBeginDateAndHourValid.Default(false) && mpParams.CurrentEndDateAndHourValid.Default(false))
				{
					if (mpParams.CurrentEndDateAndHour != null && mpParams.CurrentBeginDateAndHour > mpParams.CurrentEndDateAndHour)
					{
						Category.CheckCatalogResult = "A";
					}
					else
					{
						mpParams.CurrentDatesAndHoursConsistent = true;
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH5");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-6: Parameter Code
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH6(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				mpParams.CurrentParameterValid = false;
				if (mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodParameterCd == null)
				{
					Category.CheckCatalogResult = "A";
				}
				else
				{
					if (!LookupCodeExists(mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodParameterCd, "MATS_METHOD_PARAMETER_CD", mpParams.MatsMethodParameterCodeLookup.SourceView))
					{
						Category.CheckCatalogResult = "B";
					}
					else
					{
						mpParams.CurrentParameterValid = true;
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH6");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-7: Method Code
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH7(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				{
					mpParams.CurrentMethodValid = false;
					if (mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodCd == null)
					{
						Category.CheckCatalogResult = "A";
					}
					else
					{
						if (!LookupCodeExists(mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodCd, "MATS_METHOD_CD", mpParams.MatsMethodCodeLookup.SourceView))
						{
							Category.CheckCatalogResult = "B";
						}
						else
						{
							mpParams.CurrentMethodValid = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH7");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-8: Parameter and Method Cross Check
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH8(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				if (mpParams.CurrentMethodValid.Default(false) && mpParams.CurrentParameterValid.Default(false))
				{
					sFilterPair[] MATSCrossCheckFilter = new sFilterPair[2];
					MATSCrossCheckFilter[0].Set("ParameterCode", mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodParameterCd);
					MATSCrossCheckFilter[1].Set("MethodCode", mpParams.MatsSupplementalComplianceMethodRecord.MatsMethodCd);

					DataView MATSCrossCheckView = FindRows(mpParams.CrosscheckMatssupplementalcomplianceparametertomethod.SourceView, MATSCrossCheckFilter);
					if (MATSCrossCheckView.Count == 0)
					{
						Category.CheckCatalogResult = "A";
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH8");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-9: Hg Spans Evaluation Range
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH9(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				sFilterPair[] MATSCombinedRecordsFilter = new sFilterPair[3];
				MATSCombinedRecordsFilter[0].Set("PARAMETER_GROUP", "HG");
				MATSCombinedRecordsFilter[1].Set("BEGIN_DATEHOUR", mpParams.EvaluationEndDate.Default(DateTime.MaxValue), eFilterDataType.DateBegan, eFilterPairRelativeCompare.LessThanOrEqual);
				MATSCombinedRecordsFilter[2].Set("END_DATEHOUR", mpParams.MatsEvaluationBeginDate, eFilterDataType.DateEnded, eFilterPairRelativeCompare.GreaterThanOrEqual);
				DataView MATSCombinedRecordsView = FindRows(mpParams.MatsCombinedMethodRecordsByLocation.SourceView, MATSCombinedRecordsFilter);

				if (MATSCombinedRecordsView.Count > 0)
				//span check
				{
					if (!CheckForHourRangeCovered(Category, MATSCombinedRecordsView,
					"BEGIN_DATEHOUR", "END_DATEHOUR",
					mpParams.MatsEvaluationBeginDate.Default(DateTime.MinValue).AddHours(23),
					mpParams.EvaluationEndDate.Default(DateTime.MaxValue)))
					{
						Category.CheckCatalogResult = "B";
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH9");
			}

			return ReturnVal;
		}

		/// <summary>
		/// MATSMTH-10: HCl Spans Evaluation Range
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH10(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				sFilterPair[] MATSCombinedRecordsFilter = new sFilterPair[3];
				MATSCombinedRecordsFilter[0].Set("PARAMETER_GROUP", "HCL");
				MATSCombinedRecordsFilter[1].Set("BEGIN_DATEHOUR", mpParams.EvaluationEndDate.Default(DateTime.MaxValue), eFilterDataType.DateBegan, eFilterPairRelativeCompare.LessThanOrEqual);
				MATSCombinedRecordsFilter[2].Set("END_DATEHOUR", mpParams.MatsEvaluationBeginDate, eFilterDataType.DateEnded, eFilterPairRelativeCompare.GreaterThanOrEqual);
				DataView MATSCombinedRecordsView = FindRows(mpParams.MatsCombinedMethodRecordsByLocation.SourceView, MATSCombinedRecordsFilter);

				if (MATSCombinedRecordsView.Count > 0)
				{
					//span check
					if (!CheckForHourRangeCovered(Category, MATSCombinedRecordsView,
					"BEGIN_DATEHOUR", "END_DATEHOUR",
					mpParams.MatsEvaluationBeginDate.Default(DateTime.MinValue).AddHours(23),
					mpParams.EvaluationEndDate.Default(DateTime.MaxValue)))
					{
						Category.CheckCatalogResult = "B";
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH10");
			}

			return ReturnVal;
		}

		#endregion


		#region Checks 11 thru 20

		/// <summary>
		/// MATSMTH-11: HF Spans Evaluation Range
		/// </summary>
		/// <param name="category">The category object running the check.</param>
		/// <param name="log">obsolete</param>
		/// <returns>The error string if an error occurs.</returns>
		public string MATSMTH11(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";

			try
			{
				sFilterPair[] MATSCombinedRecordsFilter = new sFilterPair[3];
				MATSCombinedRecordsFilter[0].Set("PARAMETER_GROUP", "HF");
				MATSCombinedRecordsFilter[1].Set("BEGIN_DATEHOUR", mpParams.EvaluationEndDate.Default(DateTime.MaxValue), eFilterDataType.DateBegan, eFilterPairRelativeCompare.LessThanOrEqual);
				MATSCombinedRecordsFilter[2].Set("END_DATEHOUR", mpParams.MatsEvaluationBeginDate, eFilterDataType.DateEnded, eFilterPairRelativeCompare.GreaterThanOrEqual);
				DataView MATSCombinedRecordsView = FindRows(mpParams.MatsCombinedMethodRecordsByLocation.SourceView, MATSCombinedRecordsFilter);

				if (MATSCombinedRecordsView.Count > 0)
				//span check
				{
					if (!CheckForHourRangeCovered(Category, MATSCombinedRecordsView,
					"BEGIN_DATEHOUR", "END_DATEHOUR",
					mpParams.MatsEvaluationBeginDate.Default(DateTime.MinValue).AddHours(23),
					mpParams.EvaluationEndDate.Default(DateTime.MaxValue)))
					{
						Category.CheckCatalogResult = "B";
					}
				}
			}
			catch (Exception ex)
			{
				ReturnVal = Category.CheckEngine.FormatError(ex, "MATSMTH11");
			}

			return ReturnVal;
		}

		#endregion

	}
}
