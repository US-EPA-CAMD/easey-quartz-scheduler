using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseAccess
{

    /// ConnectionStringManager
    public static class ConnectionStringManager
    {
        private static IConfiguration Configuration { get; set; }
        private static string host { get; set; }
        private static string user { get; set; }
        private static string password { get; set; }
        private static string db { get; set; }
        private static string vcapServices { get; set; }
        private static string replicaHost { get; set; }

        //Connection options
        private static string appName { get; set; }
        private static int maxConnectionPool { get; set; }
        private static int connectionIdleLifetime { get; set; }
        private static int connectionTimeout { get; set; }
        private static int commandTimeout { get; set; }
        private static int connectionLifeTime { get; set; }
        private static int statementTimeout { get; set; }
        private static int idleInTransactionSessionTimeout { get; set; }

        private static string connectionString { get; set; }

        /// getConnectionString
        public static string getConnectionString(IConfiguration configuration)
        {
            Configuration = configuration;

            int port = 5432;
            int.TryParse(Configuration["EASEY_DB_PORT"], out port);

            host = Configuration["EASEY_DB_HOST"] ?? "localhost";
            user = Configuration["EASEY_DB_USER"] ?? "postgres";
            password = Configuration["EASEY_DB_PWD"] ?? "password";
            db = Configuration["EASEY_DB_NAME"] ?? "postgres";
            replicaHost = Configuration["EASEY_DB_REPLICA_HOST"];
            vcapServices = Configuration["VCAP_SERVICES"];

            if (!string.IsNullOrWhiteSpace(vcapServices))
            {
                dynamic vcapSvc = JsonConvert.DeserializeObject(vcapServices);
                dynamic vcapSvcCreds = vcapSvc["aws-rds"][0].credentials;

                host = vcapSvcCreds.host;
                port = vcapSvcCreds.port;
                user = vcapSvcCreds.username;
                password = vcapSvcCreds.password;
                db = vcapSvcCreds.name;
                replicaHost = vcapSvcCreds.replica_host;
            }

            appName                 = Configuration["name"] ?? "quartz-scheduler";
            maxConnectionPool       = Configuration.GetValue<int>("EASEY_DB_MAX_CONNECTION_POOL", 100);
            connectionIdleLifetime  = Configuration.GetValue<int>("EASEY_DB_IDLE_TIMEOUT", 300);
            connectionTimeout       = Configuration.GetValue<int>("EASEY_DB_CONNECTION_TIMEOUT", 15);
            commandTimeout          = Configuration.GetValue<int>("EASEY_DB_COMMAND_TIMEOUT", 300);
            connectionLifeTime      = Configuration.GetValue<int>("EASEY_DB_CONNECTION_LIFE_TIME", 1800);
            statementTimeout        = Configuration.GetValue<int>("EASEY_DB_STATEMENT_TIMEOUT", 300000);
            idleInTransactionSessionTimeout = Configuration.GetValue<int>("EASEY_DB_IDLE_TRANS_SESSION_TIMEOUT", 300000);

            // Check if replica usage is enabled via environment variable
            bool replicaEnabled = Configuration.GetValue<bool>("EASEY_QUARTZ_SCHEDULER_DB_REPLICA_ENABLED", false);
            
            // Build the Server parameter with multi-host support
            string serverParam;
            if (!string.IsNullOrWhiteSpace(replicaHost) && replicaEnabled)
            {
                // Multi-host format: Server=primary:port,replica:port
                serverParam = $"Server={host}:{port},{replicaHost}:{port}";
            }
            else
            {
                // Single-host fallback
                serverParam = $"Server={host};Port={port}";
            }

            // Build base connection string
            // Note: Target Session Attributes should NOT be in the connection string
            // - For multi-host: routing is done via WithTargetSession() on the data source
            // - For single-host: Npgsql doesn't allow Target Session Attributes at all
            connectionString = $"{serverParam};Username={user};Password={password};Database={db};Pooling=true;"
                    + $"ApplicationName={appName};"
                    + $"MaxPoolSize={maxConnectionPool};"      // Max connections in pool
                    + $"ConnectionIdleLifetime={connectionIdleLifetime};" // Close idle connections
                    + $"Timeout={connectionTimeout};"          // Maximum time (ms) to wait for a new connection before timing out.
                    + $"CommandTimeout={commandTimeout};"  //Npgsql (Client-side) kills if query (command) takes longer than this
                    + $"ConnectionLifeTime={connectionLifeTime};"  //The total maximum lifetime of connections (in seconds).
                    + $"Options='-c statement_timeout={statementTimeout} -c idle_in_transaction_session_timeout={idleInTransactionSessionTimeout}'";

            return connectionString; //PostgreSQL (db-side) kills if query takes longer than statementTimeout
        }
    }
}
