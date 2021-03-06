using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json;

namespace SilkierQuartz.Helpers
{
    public class JsonErrorResponseAttribute : ActionFilterAttribute
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new DefaultContractResolver(), // PascalCase as default
        };
        private static readonly object _systemTextSerializerOptions = new JsonSerializerOptions();

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<JsonResult>>();
                var serializerOptions = executor.GetType().FullName.Contains("SystemTextJson")
                    ? _systemTextSerializerOptions
                    : _serializerSettings;

                context.Result = new JsonResult(new { ExceptionMessage = context.Exception.Message }, serializerOptions) { StatusCode = 400 };
                context.ExceptionHandled = true;
            }
        }
    }
}
