using System.Net;
using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Quartz;
using Epa.Camd.Quartz.Scheduler.Jobs;

using Npgsql;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class NpgSqlContext : DbContext
  {
    private readonly ILogger _logger;
    public IConfiguration Configuration { get; }

    public DbSet<BulkFileMetadata> BulkFileMetadataSet {get; set; }
    public DbSet<BulkFileQueue> BulkFileQueue {get; set; }
    public DbSet<JobLog> JobLogs {get; set; }
    public DbSet<ReportingPeriod> ReportingPeriods {get; set; }
    public DbSet<BulkFileLog> BulkFileLogs {get; set; }
    public DbSet<ProgramCode> ProgramCodes { get; set; } 
    //public DbSet<EmissionEvaluation> EmissionEvaluations { get; set; } 
    public DbSet<EvaluationSet> EvaluationSet { get; set; } 
    public DbSet<Evaluation> Evaluations { get; set; }    
    public DbSet<SeverityCode> SeverityCodes { get; set; }
    public DbSet<EmissionEvaluation> EmissionEvaluations { get; set; }
    public DbSet<EvalStatusCode> EvalStatusCodes { get; set; }
    public DbSet<CheckSession> CheckSessions { get; set; }
    public DbSet<CorsOptions> CorsOptions { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<MonitorPlan> MonitorPlans { get; set; }

    public DbSet<CertEvent> CertEvents { get; set; }

    public DbSet<TestSummary> TestSummaries { get; set; }

    public DbSet<TestExtensionExemption> TestExtensionExemptions { get; set; }
    public DbSet<MonitorLocation> MonitorLocations { get; set; }
    public DbSet<MonitorPlanLocation> MonitorPlanLocations { get; set; }

    public NpgSqlContext(IConfiguration configuration, ILogger<NpgSqlContext> logger, DbContextOptions<NpgSqlContext> options) : base(options)
    {
      _logger = logger;
      Configuration = configuration;
    }
    public async Task CreateBulkFileRecord(string name, Guid parent_id, int? year, int? quarter, string stateCd, string dataType, string subType, string url, string fileName, Guid job_id, string program_code){
      try{
        Guid child_job_id = Guid.NewGuid();
        BulkFileQueue jl = new BulkFileQueue();
        jl.JobId = child_job_id;
        jl.ParentJobId = parent_id;
        jl.AddDate = Utils.getCurrentEasternTime();
        jl.StartDate = null;
        jl.EndDate = null;
        jl.StatusCd = "QUEUED";
        jl.Year = year;
        jl.Quarter = quarter;
        jl.StateCode = stateCd;
        jl.DataType = dataType;
        jl.SubType = subType;
        jl.Url = url;
        jl.FileName = fileName;
        jl.ProgramCode = program_code;
        jl.JobName = name;
        await this.BulkFileQueue.AddAsync(jl);
        await this.SaveChangesAsync();
      }catch(Exception e){
        throw new Exception(e.Message);
      }
    }

    public async Task<List<ProgramCode>> getProgramCodes(){
      return await this.ProgramCodes.ToListAsync();
    }

    public async  Task<List<List<Object>>> ExecuteSqlQuery(string commandText, int columns)
    {

      var connectionString = this.Database.GetConnectionString();
      List<List<Object>> rows = new List<List<Object>>();

      try
      {
        using (var connection = new NpgsqlConnection(connectionString))
        {
          if (connection.State != ConnectionState.Open)
            connection.Open();

          await using var cmd = new NpgsqlCommand(commandText, connection);
          await using var reader = await cmd.ExecuteReaderAsync();

          while (reader.Read())
          {
              List<Object> row = new List<Object>();
              
              for(int i = 0; i < columns; i++){
                row.Add(reader.GetProviderSpecificValue(i));
              }
                            
              rows.Add(row);
          }

          connection.Close();
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      return rows;
    }

    public async void ExecuteEmissionRefreshProcedure(string monPlanId, decimal year, decimal quarter){
      var connectionString = this.Database.GetConnectionString();
      var connection = new NpgsqlConnection(connectionString);

      if (connection.State != ConnectionState.Open)
            connection.Open();

      await using var cmd = new NpgsqlCommand("CALL camdecmpswks.refresh_emissions_views($1, $2, $3)", connection)
      {
          Parameters =
          {
              new() { Value = monPlanId },
              new() { Value = year },
              new() { Value = quarter }
          }
      };

      await cmd.ExecuteNonQueryAsync();
      connection.Close();
    }

    public void ExecuteSql(string commandText)
    {
      var connectionString = this.Database.GetConnectionString();


      Console.Write(commandText);
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
