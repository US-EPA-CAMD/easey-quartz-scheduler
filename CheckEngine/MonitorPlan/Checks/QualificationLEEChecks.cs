using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;
using ECMPS.Definitions.Extensions;

using ECMPS.Checks.Mp.Parameters;

namespace ECMPS.Checks.QualificationLEEChecks
{
	public class cQualificationLEEChecks : cChecks
	{
		public cQualificationLEEChecks()
		{
			CheckProcedures = new dCheckProcedure[4];

			CheckProcedures[1] = new dCheckProcedure(QUALLEE1);
			CheckProcedures[2] = new dCheckProcedure(QUALLEE2);
			CheckProcedures[3] = new dCheckProcedure(QUALLEE3);
		}


		#region QUALLEE1
		public string QUALLEE1(cCategory Category, ref bool Log) //Determines whether the Qualification Test Type has a valid value.
		{
			string ReturnVal = "";

			try
			{
				if (mpParams.CurrentQualificationLee.QualLeeTestTypeCd == null || !LookupCodeExists(mpParams.CurrentQualificationLee.QualLeeTestTypeCd, "QUAL_LEE_TEST_TYPE_CD", mpParams.QualificationLeeTestTypeCodeLookupTable.SourceView))
				{
					Category.CheckCatalogResult = "A";
				}
			}
			catch (Exception ex)
			{ ReturnVal = Category.CheckEngine.FormatError(ex, "QUALLEE1"); }

			return ReturnVal;
		}
		#endregion

		#region QUALLEE2
		//<summary>
		//Returns a result if the current Qualification LEE record is from an initial test but the test date for a sibling Qualification LEE record precedes the test date of the current record. 
		//Also returns a result if the current record is for a retest,
		//but no proceeding sibling record exists, or the most recent proceeding sibling's test date is more than a year prior to the current record's test date.
		//</summary>
		public string QUALLEE2(cCategory Category, ref bool Log) 
		{
			string ReturnVal = "";

			try
			{
				cFilterCondition[] QualLeeFilter = new cFilterCondition[1];
				QualLeeFilter[0] = new cFilterCondition("MON_QUAL_ID",  mpParams.CurrentQualificationLee.MonQualId);

				//find most recent record with earlier date (not <=)
				DataRowView QualLEERecent = cRowFilter.FindMostRecentRow(mpParams.QualificationleeRecords.SourceView,
												   mpParams.CurrentQualificationLee.QualTestDate.Default(DateTime.MinValue),
												   "QUAL_TEST_DATE", QualLeeFilter, eFilterConditionRelativeCompare.LessThan);
				if (QualLEERecent != null)
				{
					if (mpParams.CurrentQualificationLee.QualLeeTestTypeCd == "INITIAL")
					{
						Category.CheckCatalogResult = "A";
					}
					else if (QualLEERecent["QUAL_TEST_DATE"].AsDateTime() < ((DateTime)mpParams.CurrentQualificationLee.QualTestDate).AddYears(-1))
					{
						Category.CheckCatalogResult = "B";
					}
				}
				else if (mpParams.CurrentQualificationLee.QualLeeTestTypeCd == "RETEST")
				{
					Category.CheckCatalogResult = "C";
				}
			}
			catch (Exception ex)
			{ ReturnVal = Category.CheckEngine.FormatError(ex, "QUALLEE2"); }

			return ReturnVal;
		}
		#endregion

		#region QUALLEE3
		//<summary>
		//Returns a result if all four Qualification LEE value fields are null, if both the one Potential Emissions and at least one of the three Emission Standard fields are not null, 
		//or if one but not all three of the Emission Standard fields is not null.
		//Basically, the record should contain either Potential Emissions or Emission Standard information but not both, and if it contains Emission Standard information it should contain complete information.
		//</summary>
		public string QUALLEE3(cCategory Category, ref bool Log)
		{
			string ReturnVal = "";
			if (mpParams.CurrentQualificationLee.PotentialAnnualEmissions == null && mpParams.CurrentQualificationLee.ApplicableEmissionStandard == null && mpParams.CurrentQualificationLee.EmissionStandardPct == null)
				Category.CheckCatalogResult = "A";
			else if (mpParams.CurrentQualificationLee.PotentialAnnualEmissions != null &&
				(mpParams.CurrentQualificationLee.ApplicableEmissionStandard != null || mpParams.CurrentQualificationLee.EmissionStandardUom != null || mpParams.CurrentQualificationLee.EmissionStandardPct != null))
				Category.CheckCatalogResult = "B";
			else if (mpParams.CurrentQualificationLee.PotentialAnnualEmissions == null &&
				(mpParams.CurrentQualificationLee.ApplicableEmissionStandard == null || mpParams.CurrentQualificationLee.EmissionStandardUom == null || mpParams.CurrentQualificationLee.EmissionStandardPct == null))
				Category.CheckCatalogResult = "C";
			try
			{
			}
			catch (Exception ex)
			{ ReturnVal = Category.CheckEngine.FormatError(ex, "QUALLEE3"); }

			return ReturnVal;
		}
		#endregion

	}
}
