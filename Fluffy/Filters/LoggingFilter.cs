using System.Diagnostics;

namespace Fluffy.Filters;

public class LoggingFilter : IEndpointFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var requestPath = httpContext.Request.Path;
        var method = httpContext.Request.Method;

        _logger.LogInformation("Request started: {Method} {Path}", method, requestPath);

        var stopwatch = Stopwatch.StartNew();
        var result = await next(context);
        stopwatch.Stop();

        _logger.LogInformation("Request completed: {Method} {Path} - Took {ElapsedMilliseconds}ms", 
            method, requestPath, stopwatch.ElapsedMilliseconds);

        return result;
    }
}
