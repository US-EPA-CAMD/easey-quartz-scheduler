using System;
using System.Data;

using ECMPS.Checks.CheckEngine;
using ECMPS.Checks.Parameters;
using ECMPS.Checks.TypeUtilities;

namespace ECMPS.Checks.QAScreenEvaluation
{
  public class cLinearityRow : cCategory
  {

    #region Private Fields

    private cQAScreenMain mQAScreenMain;
    private DataTable mLinearityTable;
    private string mComponentID;
    private string mMonitorLocationID;

    #endregion

    #region Constructors

    public cLinearityRow(cCheckEngine ACheckEngine, cQAScreenMain QAScreenMain, string MonitorLocationId, string TestSumId, DataTable LinearityTable)
      : base(ACheckEngine, (cProcess)QAScreenMain, "SCRLINE", LinearityTable)
    {
      InitializeCurrent(MonitorLocationId, TestSumId);

      mQAScreenMain = QAScreenMain;
      mLinearityTable = LinearityTable;
      mMonitorLocationID = MonitorLocationId;

      FilterData();

      SetRecordIdentifier();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the checkbands for this category to the passed check bands and then executes
    /// those checks.
    /// </summary>
    /// <param name="ACheckBands">The check bands to process.</param>
    /// <returns>True if the processing of check executed normally.</returns>
    public bool ProcessChecks(cCheckParameterBands ACheckBands)
    {
      this.SetCheckBands(ACheckBands);
      return base.ProcessChecks();
    }

    #endregion

    #region Base Class Overrides

    protected override void FilterData()
    {
      SetCheckParameter("Current_Linearity_Test", new DataView(mLinearityTable,
        "", "", DataViewRowState.CurrentRows)[0], eParameterDataType.DataRowView);

      SetCheckParameter("Current_Test", new DataView(mLinearityTable,
        "", "", DataViewRowState.CurrentRows)[0], eParameterDataType.DataRowView);

      mComponentID = cDBConvert.ToString(((DataRowView)GetCheckParameter("Current_Linearity_Test").ParameterValue)["component_id"]);

      if (mComponentID == "")
        SetCheckParameter("Current_Component", null, eParameterDataType.DataRowView);
      else
        SetCheckParameter("Current_Component", new DataView(mQAScreenMain.SourceData.Tables["QAComponent"],
            "component_id = '" + mComponentID + "'", "", DataViewRowState.CurrentRows)[0], eParameterDataType.DataRowView);

      SetCheckParameter("Analyzer_Range_Records", new DataView(mQAScreenMain.SourceData.Tables["QAAnalyzerRange"],
        "component_id = '" + mComponentID + "'", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);

      SetCheckParameter("Linearity_Test_Records", new DataView(mQAScreenMain.SourceData.Tables["TestSummary"],
        "test_type_cd = 'LINE' and component_id = '" + mComponentID + "'", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);

      SetCheckParameter("Location_Test_Records", new DataView(mQAScreenMain.SourceData.Tables["TestSummary"],
        "Mon_loc_id = '" + mMonitorLocationID + "'", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);

      SetCheckParameter("QA_Supplemental_Data_Records", new DataView(mQAScreenMain.SourceData.Tables["QASuppData"],
        "", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);

      SetCheckParameter("Test_Reason_Code_Lookup_Table", new DataView(mQAScreenMain.SourceData.Tables["TestReasonCode"],
        "", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);

      SetCheckParameter("Test_Result_Code_Lookup_Table", new DataView(mQAScreenMain.SourceData.Tables["TestResultCode"],
        "", "", DataViewRowState.CurrentRows), eParameterDataType.DataView);
    }


    protected override void SetRecordIdentifier()
    {

      RecordIdentifier = "this record";

    }

    protected override bool SetErrorSuppressValues()
    {
        ErrorSuppressValues = null;
        return true;
    }

    #endregion

  }
}
