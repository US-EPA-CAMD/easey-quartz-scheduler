using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EaseyQuartz.Admin.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Quartz;
using Quartz.Impl;
using Xunit;

// Add for JobDetailImpl

namespace Epa.Camd.Quartz.Scheduler.Jobs.SubmissionWindowJob.Tests
{
    /// <summary>
    /// Tests for the Submission Window Management Job.
    /// Targeting .NET 8.
    /// </summary>
    [Collection("Submission Window Tests")]
    public sealed class SubmissionWindowManagementJobTests : IAsyncDisposable
    {
        private readonly Mock<ILogger<SubmissionWindowManagementJob>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public SubmissionWindowManagementJobTests()
        {
            _loggerMock = new Mock<ILogger<SubmissionWindowManagementJob>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            // Setup configuration with modern pattern
            var configValues = new Dictionary<string, string>
            {
                ["EASEY_QUARTZ_SCHEDULER_SMTP_HOST"] = "smtp.test.com",
                ["EASEY_QUARTZ_SCHEDULER_SMTP_PORT"] = "587",
                ["EASEY_QUARTZ_SCHEDULER_EMAIL"] = "test@test.com",
                ["EASEY_AUTH_API"] = "https://api.test.com"
            };

            _configurationMock.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => configValues.GetValueOrDefault(key));

            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);
        }

        public async ValueTask DisposeAsync()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
            await Task.CompletedTask; // Add this to satisfy the async requirement
        }

        private static IJobExecutionContext CreateJobContext(string eventType, string? windowId = null)
        {
            var dataMap = new JobDataMap();
            dataMap.Put("EventType", eventType);
            if (windowId is not null)
            {
                dataMap.Put("WindowId", windowId);
            }

            var jobDetail = new JobDetailImpl();
            jobDetail.JobDataMap = dataMap;

            var context = new Mock<IJobExecutionContext>();
            context.Setup(x => x.JobDetail).Returns(jobDetail);

            return context.Object;
        }

        private void SetupMockHttpResponse<T>(string requestUri, T response, HttpMethod? method = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            method ??= HttpMethod.Get;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri!.PathAndQuery.Contains(requestUri)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = JsonContent.Create(response, options: _jsonOptions)
                });
        }
    }
}
