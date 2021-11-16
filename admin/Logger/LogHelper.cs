using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Easey.JobScheduler.Logging
{
  public class LogHelper
  {
    public static void info(ILogger logger, string message, params LogVariable[] parameters)
    {
      try
      {
        Dictionary<string, object> scope = new Dictionary<string, object>();

        for (int i = 0; i < parameters.Length; i++)
        {
          scope.Add(parameters[i].key, parameters[i].value);
        }

        using (logger.BeginScope(scope))
        {
          logger.LogInformation(message);
        }
      }
      catch (Exception logInfoException)
      {
        logger.LogError(logInfoException.Message);
      }
    }

    public static void error(ILogger logger, string message, params LogVariable[] parameters)
    {
      Dictionary<string, object> scope = new Dictionary<string, object>();

      for (int i = 0; i < parameters.Length; i++)
      {
        scope.Add(parameters[i].key, parameters[i].value);
      }

      Guid errorId = System.Guid.NewGuid();
      scope.Add("errorId", errorId.ToString());

      using (logger.BeginScope(scope))
      {
        logger.LogError(message);
      }
      //throw new Exception(message + " | errorId: " + errorId.ToString());
    }
  }
}