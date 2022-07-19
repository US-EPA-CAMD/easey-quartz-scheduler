using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Quartz;
using Quartz.Impl.Matchers;

using SilkierQuartz;
using DatabaseAccess;

using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Jobs.Listeners;

namespace Epa.Camd.Quartz.Scheduler
{
  public class Startup
  {
    private string connectionString;
    private readonly string corsPolicy = "AllowedCORSOptions";
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
      connectionString = ConnectionStringManager.getConnectionString(configuration);
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddAppConfiguration(Configuration);

      services.AddDbContext<NpgSqlContext>(options =>
          options.UseNpgsql(connectionString)
      );

      // NpgSqlContext dbContext = services.BuildServiceProvider().GetService<NpgSqlContext>();

      // dbContext.CorsOptions.FromSqlRaw(@"
      //   SELECT key, value
      //   FROM camdaux.cors
      //   LEFT JOIN camdaux.cors_to_api
      //     USING(cors_id)
      //   LEFT JOIN camdaux.api
      //     USING(api_id)
      //   WHERE api_id IS NULL OR name = 'quartz-api'
      //   ORDER BY key, value;"
      // );

      //string[] allowedOrigins = new string[0];
      //string[] allowedMethods = new string[0];
      //string[] allowedHeaders = new string[0];

      services.AddCors(options => {
          options.AddPolicy(corsPolicy, builder => {
              builder.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
          });
      });

      services.AddSession();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc(
          "v1",
          new OpenApiInfo
          {
            Title = "Quartz Job Management OpenAPI Specification",
            Version = "v1",
          }
        );

        string host = Configuration["EASEY_QUARTZ_SCHEDULER_HOST"];
        string apiHost = Configuration["EASEY_API_GATEWAY_HOST"];

        if (!string.IsNullOrWhiteSpace(host) && host != "localhost")
        {
          c.AddServer(new OpenApiServer() {
            Url = $"https://{apiHost}",
          });
        }

        var bearerKeyScheme = new OpenApiSecurityScheme
        {
          Name = "Bearer",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey,
          Description = "Authorization by bearer request token!",
          Scheme = "Bearer",
          Reference = new OpenApiReference {
            Id = "BearerToken",
            Type = ReferenceType.SecurityScheme,
          }
        };       

        var apiKeyScheme = new OpenApiSecurityScheme {
          Name = "x-api-key",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey,
          Description = "Authorization by x-api-key request header!",
          Scheme = "ApiKeyScheme",
          Reference = new OpenApiReference {
            Id = "ApiKey",
            Type = ReferenceType.SecurityScheme,
          }
        };

        c.AddSecurityDefinition("BearerToken", bearerKeyScheme);
        c.AddSecurityDefinition("ApiKey", apiKeyScheme);
        c.AddSecurityRequirement(
          new OpenApiSecurityRequirement {{
            apiKeyScheme,
            new List<string>()
          }});
        c.AddSecurityRequirement( new OpenApiSecurityRequirement {{
            bearerKeyScheme,
            new List<string>()
        }});
      });

      services.AddRazorPages();
      services.AddControllers();
      
      
      services.AddSilkierQuartz(options =>
      {
        options.VirtualPathRoot = "/quartz";
        options.UseLocalTime = true;
        options.DefaultDateFormat = "yyyy-MM-dd";
        options.DefaultTimeFormat = "HH:mm:ss";
        options.CronExpressionOptions = new CronExpressionDescriptor.Options()
        {
          DayOfWeekStartIndexZero = false //Quartz uses 1-7 as the range
        };
      },
      authenticationOptions =>
      {
        authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated;
      },
      nameValueCollection =>
      {
        var quartzConfig = Configuration.GetSection("Quartz").GetChildren().GetEnumerator();

        while (quartzConfig.MoveNext())
        {
          nameValueCollection.Set(quartzConfig.Current.Key, quartzConfig.Current.Value);
        }
        nameValueCollection.Set("quartz.dataSource.default.connectionString", connectionString);
      });
      

      services.AddOptions();

      BulkFileJobQueue.RegisterWithQuartz(services);
      AllowanceComplianceBulkDataFiles.RegisterWithQuartz(services);
      EmissionsComplianceBulkDataFiles.RegisterWithQuartz(services);
      AllowanceTransactionsBulkDataFiles.RegisterWithQuartz(services);
      FacilityAttributesBulkDataFiles.RegisterWithQuartz(services);
      BulkDataFile.RegisterWithQuartz(services);
      BulkDataFileMaintenance.RegisterWithQuartz(services);
      ApportionedEmissionsBulkData.RegisterWithQuartz(services);
      SendMail.RegisterWithQuartz(services);
      CheckEngineEvaluation.RegisterWithQuartz(services);
      RemoveExpiredUserSession.RegisterWithQuartz(services);
      RemoveExpiredCheckoutRecord.RegisterWithQuartz(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
      }

      app.UseSession();
      app.UseStaticFiles();
      app.UseRouting();
      app.UseCors(corsPolicy);
      app.UseAuthentication();
      app.UseAuthorization();
      
      bool displayFlag = bool.Parse(Configuration["EASEY_QUARTZ_SCHEDULER_DISPLAY_UI"]);
      app.UseSilkierQuartz(displayUi: displayFlag);

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });

      string apiPath = Configuration["EASEY_QUARTZ_SCHEDULER_API_PATH"];

      app.UseSwagger(c =>
      {
        c.RouteTemplate = apiPath + "/swagger/{documentname}/swagger.json";
      });

      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint($"./v1/swagger.json", "Quartz API v1");
        c.RoutePrefix = $"{apiPath}/swagger";
      });

      IScheduler scheduler = app.GetScheduler();

      BulkDataFile.setScheduler(scheduler);

      scheduler.ListenerManager.AddJobListener(
          new CheckEngineEvaluationListener(Configuration),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("MP")),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("QA")),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("EM"))
      );

      BulkFileJobQueue.ScheduleWithQuartz(scheduler, app);
      //AllowanceComplianceBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      //EmissionsComplianceBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      //AllowanceTransactionsBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      //FacilityAttributesBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      //ApportionedEmissionsBulkData.ScheduleWithQuartz(scheduler, app);
      //BulkDataFileMaintenance.ScheduleWithQuartz(scheduler, app);
      RemoveExpiredUserSession.ScheduleWithQuartz(scheduler, app);
      RemoveExpiredCheckoutRecord.ScheduleWithQuartz(scheduler, app);      
    }
  }
}
