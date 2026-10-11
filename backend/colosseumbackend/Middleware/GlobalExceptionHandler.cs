using colosseumbackend.Common;
using colosseumbackend.Solana;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Middleware;

/// <summary>Maps unexpected exceptions to problem details; full detail only in Development.</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment                _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger      = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        int status = exception switch
        {
            SolanaUnavailableException => StatusCodes.Status503ServiceUnavailable,
            DbUpdateException          => StatusCodes.Status409Conflict,
            _                          => StatusCodes.Status500InternalServerError,
        };
        string title = status == StatusCodes.Status503ServiceUnavailable ? ResponseMessages.ChainUnavailable : "An unexpected error occurred";

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title  = title,
            Detail = _environment.IsDevelopment() ? exception.ToString() : null,
        }, cancellationToken);

        return true;
    }
}
