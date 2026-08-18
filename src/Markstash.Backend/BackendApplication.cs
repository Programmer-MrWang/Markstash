using Markstash.Backend.Api;
using Markstash.Backend.Runtime;
using Markstash.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Markstash.Backend;

public static class BackendApplication
{
    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMarkstashBackend(builder.Configuration);
        return builder;
    }

    public static WebApplication Build(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var app = builder.Build();
        _ = app.Services.GetRequiredService<ServerRuntimeState>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            var statusCode = httpContext.Response.StatusCode;
            var problemDetailsService = httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = ReasonPhrases.GetReasonPhrase(statusCode),
                    Type = $"/problems/http-{statusCode}",
                },
            });
        });

        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapMarkstashApi();
        return app;
    }
}
