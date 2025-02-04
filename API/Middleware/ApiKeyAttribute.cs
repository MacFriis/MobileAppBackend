using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Middleware;

public class ApiKeyAttribute : Attribute, IActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Tjek om [AllowAnonymous] er anvendt
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return; // Spring API Key-validering over
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            context.Result = new ContentResult
            {
                StatusCode = 401,
                Content = "API Key is missing"
            };
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
        if (configuration != null)
        {
            var expectedApiKey = configuration.GetValue<string>(ApiKeyHeaderName);
            if (expectedApiKey != null && expectedApiKey.Equals(apiKey)) return;
        }

        context.Result = new ContentResult
        {
            StatusCode = 401,
            Content = "Invalid API Key"
        };
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
