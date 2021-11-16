using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Quartz;
using Quartz.Job;
using Quartz.Impl.Matchers;

using SilkierQuartz;
using DatabaseAccess;

using Epa.Camd.Easey.JobScheduler.Jobs;
using Epa.Camd.Easey.JobScheduler.Models;
using Epa.Camd.Easey.JobScheduler.Jobs.Listeners;

namespace Epa.Camd.Easey.JobScheduler
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

            services.AddSwaggerGen(c => {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo {
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
            nameValueCollection => {
                var quartzConfig = Configuration.GetSection("Quartz").GetChildren().GetEnumerator();

                while (quartzConfig.MoveNext())
                {
                    nameValueCollection.Set(quartzConfig.Current.Key, quartzConfig.Current.Value);
                }
                nameValueCollection.Set("quartz.dataSource.default.connectionString", connectionString);
            });

            services.AddOptions();

            services.AddQuartzJob<SendMailJob>(
                new JobKey(
                    Constants.JobDetails.SEND_EMAIL_KEY,
                    Constants.JobDetails.SEND_EMAIL_GROUP
                ),
                Constants.JobDetails.SEND_EMAIL_DESCRIPTION
            );

            services.AddQuartzJob<CheckEngineEvaluation>(
                CheckEngineEvaluation.WithJobKey(),
                Constants.JobDetails.CHECK_ENGINE_EVALUATION_DESCRIPTION
            );

            services.AddQuartzJob<RemoveExpiredUserSession>(
                RemoveExpiredUserSession.WithJobKey(), 
                Constants.JobDetails.EXPIRED_USER_SESSIONS_DESCRIPTION
            );
            
            services.AddQuartzJob<RemoveExpiredCheckoutRecord>(
                RemoveExpiredCheckoutRecord.WithJobKey(), 
                Constants.JobDetails.EXPIRED_CHECK_OUTS_DESCRIPTION
            );

            services.AddAppConfiguration(Configuration);
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
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSilkierQuartz();

            //app.UseCors("CorsPolicy");

             app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger(c => {
                c.RouteTemplate = "quartz/api/swagger/{documentname}/swagger.json";
            });

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/quartz/api/swagger/v1/swagger.json", "v1");
                c.RoutePrefix = "quartz/api/swagger";
            });

            IScheduler scheduler = app.GetScheduler();
            
            if (!await scheduler.CheckExists(
                    RemoveExpiredUserSession.WithJobKey()
            ))
            {
                app.UseQuartzJob<RemoveExpiredUserSession>(
                    RemoveExpiredUserSession.WithCronSchedule("0 0/15 * ? * * *")
                );
            }

            if (!await scheduler.CheckExists(
                    RemoveExpiredCheckoutRecord.WithJobKey()
            ))
            {
                app.UseQuartzJob<RemoveExpiredCheckoutRecord>(
                    RemoveExpiredCheckoutRecord.WithCronSchedule("0 0/15 * ? * * *")
                );
            }

            app.GetScheduler().ListenerManager.AddJobListener(
                new CheckEngineEvaluationListener(Configuration),
                KeyMatcher<JobKey>.KeyEquals(
                    new JobKey(
                        Constants.JobDetails.CHECK_ENGINE_EVALUATION_KEY,
                        Constants.JobDetails.CHECK_ENGINE_EVALUATION_GROUP
                    )
                )
            );
        
        }
    }
}



