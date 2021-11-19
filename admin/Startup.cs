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
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
      connectionString = ConnectionStringManager.getConnectionString(configuration);
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      // services.AddCors(options => {
      //     options.AddPolicy("CorsPolicy", builder => {
      //         builder.WithOrigins(
      //             "http://localhost:3000",
      //             "https://localhost:3000",
      //             "https://easey-dev.app.cloud.gov",
      //             "https://easey-tst.app.cloud.gov"
      //         )
      //         .AllowAnyMethod()
      //         .AllowAnyHeader();
      //     });
      // });
      services.AddSession();
      services.AddDbContext<NpgSqlContext>(options =>
          options.UseNpgsql(connectionString)
      );

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

      SendMail.RegisterWithQuartz(services);
      CheckEngineEvaluation.RegisterWithQuartz(services);
      RemoveExpiredUserSession.RegisterWithQuartz(services);
      RemoveExpiredCheckoutRecord.RegisterWithQuartz(services);

      services.AddAppConfiguration(Configuration);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
      app.UseAuthentication();
      app.UseAuthorization();
      app.UseSilkierQuartz();

      //app.UseCors("CorsPolicy");

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });

      app.UseSwagger(c =>
      {
        c.RouteTemplate = "quartz/api/swagger/{documentname}/swagger.json";
      });

      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/quartz/api/swagger/v1/swagger.json", "v1");
        c.RoutePrefix = "quartz/api/swagger";
      });

      IScheduler scheduler = app.GetScheduler();

      scheduler.ListenerManager.AddJobListener(
          new CheckEngineEvaluationListener(Configuration),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("MP")),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("QA")),
          KeyMatcher<JobKey>.KeyEquals(CheckEngineEvaluation.WithJobKey("EM"))
      );

      RemoveExpiredUserSession.ScheduleWithQuartz(scheduler, app);
      RemoveExpiredCheckoutRecord.ScheduleWithQuartz(scheduler, app);      
    }
  }
}
