using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineGame.InventoryService.Api.Contracts.Common;

namespace OnlineGame.InventoryService.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception while processing request.");

        var error = new ApiErrorResponse
        {
            Code = "INTERNAL_ERROR",
            Message = "An unexpected error occurred."
        };

        var result = new ObjectResult(error)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        await result.ExecuteResultAsync(new ActionContext
        {
            HttpContext = httpContext
        });

        return true;
    }
}
