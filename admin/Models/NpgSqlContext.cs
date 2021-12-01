using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class NpgSqlContext : DbContext
  {
    private readonly ILogger _logger;
    public IConfiguration Configuration { get; }
    public DbSet<CorsOptions> CorsOptions { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<MonitorPlan> MonitorPlans { get; set; }
    public DbSet<MonitorLocation> MonitorLocations { get; set; }
    public DbSet<MonitorPlanLocation> MonitorPlanLocations { get; set; }

    public NpgSqlContext(IConfiguration configuration, ILogger<NpgSqlContext> logger, DbContextOptions<NpgSqlContext> options) : base(options)
    {
      _logger = logger;
      Configuration = configuration;
    }

    public void ExecuteSql(string commandText)
    {
      var connectionString = this.Database.GetConnectionString();

      try
      {
        using (var connection = new NpgsqlConnection(connectionString))
        {
          if (connection.State != ConnectionState.Open)
            connection.Open();

          using (var cmd = new NpgsqlCommand(commandText, connection))
          {
            cmd.ExecuteNonQuery();
          }

          connection.Close();
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
    }

    public void ExecuteSql(string tableName, string commandText, DataSet ds)
    {
      var connectionString = this.Database.GetConnectionString();

      using (var connection = new NpgsqlConnection(connectionString))
      {
        if (connection.State != ConnectionState.Open)
          connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        DataTable dt = new DataTable(tableName);
        NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
        da.Fill(dt);
        ds.Tables.Add(dt);

        connection.Close();
      }
    }

    public NpgsqlCommand ExecuteProcedure(string name, List<NpgsqlParameter> parameters)
    {
      var connectionString = this.Database.GetConnectionString();
      var connection = new NpgsqlConnection(connectionString);

      if (connection.State != ConnectionState.Open)
        connection.Open();

      var command = connection.CreateCommand();
      command.CommandText = name;
      command.CommandType = CommandType.StoredProcedure;

      foreach (var param in parameters)
      {
        command.Parameters.Add(param);
      }

      command.ExecuteNonQuery();
      connection.Close();

      return command;
    }

    public NpgsqlParameter CreateParameter(string name, string value, NpgsqlTypes.NpgsqlDbType type, System.Data.ParameterDirection direction)
    {
      object oValue = value;
      if (string.IsNullOrEmpty(value))
      {
        oValue = System.DBNull.Value;
      }

      NpgsqlParameter param = new NpgsqlParameter();
      param.Value = oValue;
      param.ParameterName = name;
      param.NpgsqlDbType = type;
      param.Direction = direction;
      return param;
    }
  }
}
