using System;
using EaseyQuartz.Admin.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Epa.Camd.Quartz.Scheduler.Jobs.SubmissionWindowJob.Extensions
{
    public static class SubmissionWindowJobExtensions
    {
        public static IServiceCollection RegisterWithQuartz(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddQuartz(q =>
            {
                // Create job keys
                var submissionReminderJobKey = new JobKey("SubmissionReminderJob", "SubmissionWindows");
                var windowNotificationJobKey = new JobKey("WindowNotificationJob", "SubmissionWindows");
                var closeWindowJobKey = new JobKey("CloseWindowJob", "SubmissionWindows");

                // Register jobs (without triggers - they will be managed through the UI)
                q.AddJob<SubmissionWindowManagementJob>(submissionReminderJobKey, j => j
                    .WithDescription("Job for handling submission reminders (Events 151, 152)")
                    .UsingJobData("EventType", "SUBMISSIONREMINDER")
                    .StoreDurably() // Allow the job to exist without triggers
                );

                q.AddJob<SubmissionWindowManagementJob>(windowNotificationJobKey, j => j
                    .WithDescription("Job for handling window notifications (Events 155, 156)")
                    .UsingJobData("EventType", "WINDOWNOTIFICATION")
                    .StoreDurably() // Allow the job to exist without triggers
                );

                q.AddJob<SubmissionWindowManagementJob>(closeWindowJobKey, j => j
                    .WithDescription("Job for closing submission windows and sending notifications")
                    .UsingJobData("EventType", "CLOSEWINDOW")
                    .StoreDurably() // Allow the job to exist without triggers
                );

                // Configure instance name and job store
                q.SchedulerName = "ECMPS Submission Window Scheduler";

                // Configure JSON serialization
                q.SetProperty("quartz.serializer.type", "json");

                // Configure thread pool
                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 10;
                });
            });

            // Add Quartz.NET hosted service with .NET 8 configuration
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
            });

            // Configure Quartz options if UI is enabled
            if (Environment.GetEnvironmentVariable("EASEY_QUARTZ_SCHEDULER_DISPLAY_UI")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.Configure<QuartzHostedServiceOptions>(options =>
                {
                    options.WaitForJobsToComplete = true;
                });
            }

            return services;
        }

        /// <summary>
        /// Example cron expressions for reference when creating triggers in the UI
        /// </summary>
        public static class CronExpressions
        {
            /// <summary>
            /// Run at 9 AM every weekday (Monday through Friday)
            /// </summary>
            public const string WeekdayMorning = "0 0 9 ? * MON-FRI";

            /// <summary>
            /// Run at 5 PM every weekday (Monday through Friday)
            /// </summary>
            public const string WeekdayEvening = "0 0 17 ? * MON-FRI";

            /// <summary>
            /// Run at 11 PM every day
            /// </summary>
            public const string EndOfDay = "0 0 23 ? * *";

            /// <summary>
            /// Run at 7 AM every day
            /// </summary>
            public const string StartOfDay = "0 0 7 ? * *";
        }
    }
}
