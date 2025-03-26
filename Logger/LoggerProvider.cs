using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Logger
{

  /// <summary>
  /// Provides access to application-wide loggers in classes that are not created through dependency injection (DI).
  /// </summary>
  /// <remarks>
  /// Logging is typically provided via constructor injection using ILogger&lt;T&gt;.
  /// However, legacy or utility classes that are instantiated using the <c>new</c> keyword do not support DI.
  ///
  /// This static class bridges that gap by exposing access to the application's configured <see cref="ILoggerFactory"/>,
  /// allowing non-DI classes to obtain properly configured loggers (including support for Serilog, structured logging,
  /// and environment-driven logging levels).
  ///
  /// This class must be initialized once at application startup by calling <see cref="Configure"/>.
  /// See admin\Program.cs and CheckEngineRunner\Program.cs
  /// </remarks>

  public static class LoggerProvider
  {
      private static ILoggerFactory _loggerFactory;

      public static void Configure(ILoggerFactory loggerFactory)
      {
          _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      }

      public static ILogger<T> GetLogger<T>() =>
          _loggerFactory.CreateLogger<T>();

      public static ILogger GetLogger(string category) =>
          _loggerFactory.CreateLogger(category);
  }
}