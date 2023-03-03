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

        private static string connectionString { get; set; }


        /// getConnectionString
        public static string getConnectionString(IConfiguration configuration)
        {
            Configuration = configuration;

            int port = 5432;
            int.TryParse(Configuration["EASEY_DB_PORT"], out port);


            port = 15240;

            host =  "localhost";   //Configuration["EASEY_DB_HOST"] ?? "database";
            user =  "usMu5U0tyGW3dWyN";   //Configuration["EASEY_DB_USER"] ?? "postgres";
            password = "cSvB92pgyR8rqH1l7ZMqe3Vii"; //Configuration["EASEY_DB_PWD"] ?? "password";
            db =  "cgawsbrokerprod4dsl7fy3";     //Configuration["EASEY_DB_NAME"] ?? "postgres";
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
            }

            return connectionString = $"server={host};port={port};user id={user};password={password};database={db};pooling=true";
        }
    }
}
