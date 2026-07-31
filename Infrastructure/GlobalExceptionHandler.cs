using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace aspnet_core_tutorial.Infrastructure
{
    /// <summary>
    /// Centralized handler that logs every unhandled exception reaching the ASP.NET Core
    /// exception handling middleware, with structured, correlatable details.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/13/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/13/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} (TraceId: {TraceId})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);

            // Not fully handled here: returning false lets ExceptionHandlerOptions.ExceptionHandlingPath
            // re-execute the pipeline at "/Home/Error" so the user still sees the friendly error view.
            return ValueTask.FromResult(false);
        }
    }
}
