using System;
using System.Collections.Generic;
using System.Text;
using Epa.Camd.Logger;
using Microsoft.Extensions.Logging;

namespace ECMPS.Checks.Parameters
{
  /// <summary>
  /// The base for all Process objects
  /// </summary>
  public class cCheckProcess
  {

    /// <summary>
    /// The ILogger instance to use.
    /// </summary>
    protected readonly ILogger _logger;

    #region Constructors

    /// <summary>
    /// Instantiates a cCheckProcess object
    /// </summary>
    /// <param name="AProcessCd">The process code associated with the check process object</param>
    public cCheckProcess(string AProcessCd)
    {
      _logger = LoggerProvider.GetLogger(GetType().FullName);
      FProcessCd = AProcessCd;
    }


    /// <summary>
    /// Instantiates a cCheckProcess object primarily for unit testing purposes.
    /// </summary>
    protected cCheckProcess()
    {
      _logger = LoggerProvider.GetLogger(GetType().FullName);
    }

    #endregion


    #region Public Properties

    #region Property Fields

    private string FProcessCd;

    #endregion

    /// <summary>
    /// The process code of the category object instance.
    /// </summary>
    public string ProcessCd { get { return FProcessCd; } }

    #endregion

 }
}
