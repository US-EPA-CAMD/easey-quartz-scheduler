﻿using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Data.Ecmps.Dbo.View;
using ECMPS.Checks.Em.Parameters;
using ECMPS.ErrorSuppression;


namespace ECMPS.Checks.EmissionsReport
{

    public class ComponentAuditCategory : cCategoryHourly
    {

        #region Constructors
       
        /// <summary>
        /// Creates a category with a specific parent category and category code.
        /// </summary>
        /// <param name="parentCategory">The parent category of the new category.</param>
        /// <param name="categoryCd">The category code of the new category.</param>
        /// <param name="componentIdentifier">The component identifier associated with the test.</param>
        /// <param name="componentTypeCd">The type of the component associated with the test.</param>
        /// <param name="testDateHour">The date and hour of the test.</param>
        public ComponentAuditCategory(cCategory parentCategory, ref EmParameters emparams)
            : base(parentCategory, "CMPAUDT", ref emparams)
        {
          
        }

        #endregion


        #region Methods

        public bool RunChecks()
        {
            bool result = true;

            if (Process.SourceData.Tables.Contains("COMPONENT"))
            {
                foreach (DataRowView componentRow in Process.SourceData.Tables["COMPONENT"].DefaultView)
                {
                    emParams.ComponentRecordForAudit = new VwMpComponentRow(componentRow);

                    try
                    {
                        result = ProcessChecks(emParams.ComponentRecordForAudit.MonLocId, emParams.LocationPositionLookup[emParams.ComponentRecordForAudit.MonLocId]) && result;

                        EraseParameters();
                    }
                    catch (Exception ex)
                    {
                        Process.UpdateErrors("Component Audit - [" + emParams.ComponentRecordForAudit.ComponentId + "]: " + ex.Message);
                    }
                }
            }

            return result;
        }

        #endregion


        #region Base Class Overrides

        protected override void FilterData()
        {
        }

        protected override int[] GetDataBorderModcList()
        {
            return null;
        }

        protected override int[] GetQualityAssuranceHoursModcList()
        {
            return null;
        }

        protected override void SetRecordIdentifier()
        {
            if (emParams.ComponentRecordForAudit != null)
                RecordIdentifier = emParams.ComponentRecordForAudit.ComponentIdentifier;
            else
                RecordIdentifier = null;
        }

        protected override bool SetErrorSuppressValues()
        {
            ErrorSuppressValues = new cErrorSuppressValues(CheckEngine.FacilityID,
                                                           emParams.ComponentRecordForAudit.LocationName, 
                                                           "COMPTYP", emParams.ComponentRecordForAudit.ComponentTypeCd, 
                                                           "QUARTER", CheckEngine.ReportingPeriod.BeganDate);
            return true;
        }

        #endregion

    }

}
