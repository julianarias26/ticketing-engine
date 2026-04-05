using System.Text.Json;
using FluentValidation;
using TicketingEngine.Domain.Exceptions;

namespace TicketingEngine.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleAsync(ctx, ex);
        }
    }

    private static Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, errors) = ex switch
        {
            ValidationException ve => (400, "Validation failed",
                ve.Errors.Select(e => e.ErrorMessage).ToArray()),
            SeatAlreadyReservedException => (409, ex.Message, Array.Empty<string>()),
            SeatNotFoundException        => (404, ex.Message, Array.Empty<string>()),
            OrderNotFoundException       => (404, ex.Message, Array.Empty<string>()),
            EventNotFoundException       => (404, ex.Message, Array.Empty<string>()),
            UnauthorizedAccessException  => (403, ex.Message, Array.Empty<string>()),
            DomainException              => (422, ex.Message, Array.Empty<string>()),
            _                            => (500, "An unexpected error occurred.",
                                               Array.Empty<string>())
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = status;

        var body = JsonSerializer.Serialize(new
        {
            title,
            status,
            errors,
            traceId = ctx.TraceIdentifier
        });

        return ctx.Response.WriteAsync(body);
    }
}
