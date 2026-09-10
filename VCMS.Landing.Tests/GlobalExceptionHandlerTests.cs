using System.Text.Json;
using LandingCms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LandingCms.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Json_request_returns_safe_problem_details_with_trace_id()
    {
        var services = new ServiceCollection()
            .AddProblemDetails()
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = "trace-test"
        };
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            services.GetRequiredService<IProblemDetailsService>());

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("sensitive internal detail"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var problem = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("trace-test", problem.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("sensitive internal detail", problem.RootElement.ToString());
    }

    [Fact]
    public async Task Html_request_is_forwarded_to_the_configured_error_page()
    {
        var services = new ServiceCollection()
            .AddProblemDetails()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers.Accept = "text/html";
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            services.GetRequiredService<IProblemDetailsService>());

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("test"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
