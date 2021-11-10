using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SilkierQuartz;
using Epa.Camd.Easey.RulesApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using DatabaseAccess;
//using Epa.Camd.Easey.JobScheduler.Jobs;
using System.Collections.Generic;
using System.Reflection;

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
                authenticationOptions.UserName = "admin";
                authenticationOptions.UserPassword = "password";
                authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated;
            },
            nameValueCollection => {
                var quartzConfig = Configuration.GetSection("Quartz").GetChildren().GetEnumerator();

                while (quartzConfig.MoveNext())
                {
                    nameValueCollection.Set(quartzConfig.Current.Key, quartzConfig.Current.Value);
                }
                nameValueCollection.Set("quartz.dataSource.default.connectionString", connectionString);
            }
            );
            services.AddOptions();
            //services.AddQuartzJob<cCheckEngine>();
            //services.AddQuartzJob<RemoveExpiredRecord>();
            services.AddQuartzJob<Epa.Camd.Easey.JobScheduler.Jobs.HelloWorld>();
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

             //app.UseQuartzJob<RemoveExpiredRecord>(TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()));
        }
    }
}



