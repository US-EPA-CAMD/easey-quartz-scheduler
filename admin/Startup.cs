using System;
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
using Epa.Camd.Quartz.Scheduler.Models;

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
      AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

      Utils.Configuration = Configuration;

      services.AddAppConfiguration(Configuration);

      services.AddDbContext<NpgSqlContext>(options =>
        options.UseNpgsql(connectionString)
      );

      NpgSqlContext dbContext = services.BuildServiceProvider().GetService<NpgSqlContext>();
      List<CorsOptions> options =  dbContext.CorsOptions.ToListAsync<CorsOptions>().Result;

      List<string> allowedOrigins = new List<string>();
      List<string> allowedMethods = new List<string>();
      List<string> allowedHeaders = new List<string>();

      if (Configuration["EASEY_QUARTZ_SCHEDULER_ENV"] != "production") {
          allowedOrigins.Add("http://localhost:3000");
      }

      foreach(CorsOptions opts in options){
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
        options.AddPolicy(corsPolicy, builder => {
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
        var quartzConfig = Configuration.GetSection("Quartz").GetChildren().GetEnumerator();

        while (quartzConfig.MoveNext())
        {
          nameValueCollection.Set(quartzConfig.Current.Key, quartzConfig.Current.Value);
        }
        nameValueCollection.Set("quartz.dataSource.default.connectionString", connectionString);
      });

      services.AddOptions();

      BulkDataFile.RegisterWithQuartz(services);
      BulkFileJobQueue.RegisterWithQuartz(services);
      BulkDataFileMaintenance.RegisterWithQuartz(services);
      ApportionedEmissionsBulkData.RegisterWithQuartz(services);
      AllowanceHoldingsBulkDataFiles.RegisterWithQuartz(services);
      AllowanceTransactionsBulkDataFiles.RegisterWithQuartz(services);
      AllowanceComplianceBulkDataFiles.RegisterWithQuartz(services);
      EmissionsComplianceBulkDataFiles.RegisterWithQuartz(services);
      FacilityAttributesBulkDataFiles.RegisterWithQuartz(services);
      //EvaluationJobQueue.RegisterWithQuartz(services);
      //CheckEngineEvaluation.RegisterWithQuartz(services);
      //SendMail.RegisterWithQuartz(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      Console.WriteLine("Configuring Quartz");

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

      app.Use(async (context, next) => {
        context.Response.Headers.Add("Vary", "Origin");
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Add("Pragma", "no-cache");
        context.Response.Headers.Add("Expires", "0");
        await next();
      });

      Console.WriteLine("Attempting to schedule quartz jobs");

      IScheduler scheduler = app.GetScheduler();

      BulkDataFile.setScheduler(scheduler);

      // scheduler.ListenerManager.AddJobListener(
      //   new CheckEngineEvaluationListener(Configuration),
      //   GroupMatcher<JobKey>.GroupEquals(Constants.QuartzGroups.EVALUATIONS)
      // );

      BulkFileJobQueue.ScheduleWithQuartz(scheduler, app);
      BulkDataFileMaintenance.ScheduleWithQuartz(scheduler, app);
      ApportionedEmissionsBulkData.ScheduleWithQuartz(scheduler, app);
      AllowanceHoldingsBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      AllowanceTransactionsBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      AllowanceComplianceBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      EmissionsComplianceBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      FacilityAttributesBulkDataFiles.ScheduleWithQuartz(scheduler, app);
      //EvaluationJobQueue.ScheduleWithQuartz(scheduler, app);      
    }
  }
}
