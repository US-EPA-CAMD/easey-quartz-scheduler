using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Quartz;

using SilkierQuartz;
using DatabaseAccess;

using Epa.Camd.Quartz.Scheduler.Jobs;
using Epa.Camd.Quartz.Scheduler.Jobs.EmailQueueJobs;
using Epa.Camd.Quartz.Scheduler.Models;
using Epa.Camd.Quartz.Scheduler.Jobs.Listeners;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Epa.Camd.Quartz.Scheduler
{
  public class Startup
  {
    private string _connectionString;
    private static readonly string s_corsPolicy = "AllowedCORSOptions";
    private IConfiguration _configuration { get; }

    public Startup(IConfiguration configuration)
    {
      _configuration = configuration;
      _connectionString = ConnectionStringManager.getConnectionString(configuration);
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

      Utils.Configuration = _configuration;

      services.AddAppConfiguration(_configuration);

      services.AddDbContextFactory<NpgSqlContext>(options =>
        options.UseNpgsql(_connectionString)
      );
      services.AddScoped<NpgSqlContext>(sp =>
        sp.GetRequiredService<IDbContextFactory<NpgSqlContext>>().CreateDbContext()
      );

#pragma warning disable ASP0000 
      // Ignore warning [ASP0000]:
      // >
      // > "Calling 'BuildServiceProvider' from application code results in an additional copy of singleton services being created."
      // >
      // We want to access the `NpgSqlContext` here to retrieve the CORS options and job configurations from the database.
      List<CorsOptions> corsOptions;
      using (var scope = services.BuildServiceProvider().CreateScope()) // Build a scoped service provider to access the DbContext
      {
        NpgSqlContext dbContext = scope.ServiceProvider.GetRequiredService<NpgSqlContext>();
        corsOptions = dbContext.CorsOptions.ToList();
      }
#pragma warning restore ASP0000

      List<string> allowedOrigins = new List<string>();
      List<string> allowedMethods = new List<string>();
      List<string> allowedHeaders = new List<string>();

      if (_configuration["EASEY_QUARTZ_SCHEDULER_ENV"] != "production") {
          allowedOrigins.Add("http://localhost:3000");
      }

      foreach(CorsOptions opts in corsOptions){
        switch(opts.Key){
          case "origin":
            allowedOrigins.Add(opts.Value);
            break;
          case "header":
            allowedHeaders.Add(opts.Value);
            break;
          case "method":
            allowedMethods.Add(opts.Value);
            break;
        }
      }

      services.AddCors(options => {
        options.AddPolicy(s_corsPolicy, builder => {
          builder.WithOrigins(allowedOrigins.ToArray())
            .WithHeaders(allowedHeaders.ToArray())
            .WithMethods(allowedMethods.ToArray());
        });
      });

      services.AddSession();
      services.AddRazorPages();
    
      services.AddSilkierQuartz(options => {
        options.VirtualPathRoot = "/quartz";
        options.UseLocalTime = true;
        options.DefaultDateFormat = "yyyy-MM-dd";
        options.DefaultTimeFormat = "HH:mm:ss";
        options.CronExpressionOptions = new CronExpressionDescriptor.Options()
        {
          DayOfWeekStartIndexZero = false //Quartz uses 1-7 as the range
        };
      },
      authenticationOptions => {
        authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated;
      },
      nameValueCollection => {
        var quartzConfig = _configuration.GetSection("Quartz").GetChildren().GetEnumerator();

        while (quartzConfig.MoveNext())
        {
          nameValueCollection.Set(quartzConfig.Current.Key, quartzConfig.Current.Value);
        }
        nameValueCollection.Set("quartz.dataSource.default.connectionString", _connectionString);
      });

      services.AddOptions();

      CheckEngineEvaluation.RegisterWithQuartz(services);
      BulkDataFile.RegisterWithQuartz(services);
      ProcessSubmissionReminders.RegisterWithQuartz(services);
      ProcessWindowNotifications.RegisterWithQuartz(services);

      // Inject a configured HttpClient for the `BulkDataFile` job.
      // AddHttpClient<T> MUST be the last DI registration for BulkDataFile.
      services.AddHttpClient<BulkDataFile>(client =>
      {
          client.Timeout = TimeSpan.FromMilliseconds(1830000);
      });

      List<JobDescriptor> jobDescriptors = JobRegistry.BuildRegistry(typeof(DynamicJobScheduler).Assembly);
      DynamicJobScheduler.RegisterWithQuartz(services, jobDescriptors);

      services.AddTransient<CheckEngineEvaluationListener>(); //DI for CheckEngineListener
      CheckEngineEvaluationListener.ServiceCollection = services; // Set service collection of the listener
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
      logger.LogInformation("Configuring Quartz");

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
      app.UseCors(s_corsPolicy);
      app.UseAuthentication();
      app.UseAuthorization();
      
      bool displayFlag = bool.Parse(_configuration["EASEY_QUARTZ_SCHEDULER_DISPLAY_UI"]);
      app.UseSilkierQuartz(displayUi: displayFlag);

      app.Use(async (context, next) => {
        context.Response.Headers.Append("Vary", "Origin");
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Expires", "0");
        await next();
      });

      logger.LogInformation("Attempting to schedule quartz jobs");

      IScheduler scheduler = app.GetScheduler();

      BulkDataFile.setScheduler(scheduler);

      await DynamicJobScheduler.ScheduleWithQuartz(scheduler, app, logger);

      //Schedule Listeners
      CheckEngineEvaluationListener.ScheduleWithQuartz(scheduler);
    }
  }
}
