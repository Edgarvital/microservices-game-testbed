using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineGame.AuthService.Api.Contracts.Common;

namespace OnlineGame.AuthService.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);

        var errorResponse = new ApiErrorResponse
        {
            Code = "INTERNAL_SERVER_ERROR",
            Message = "Um erro inesperado ocorreu em nossos servidores. Tente novamente mais tarde."
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true; 
    }
}