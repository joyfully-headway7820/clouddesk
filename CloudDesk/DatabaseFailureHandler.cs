using Npgsql;

namespace CloudDesk;

public sealed class DatabaseFailureHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseFailureHandler> _logger;

    public DatabaseFailureHandler(RequestDelegate next, ILogger<DatabaseFailureHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            _logger.LogError(
                "Database request failed: {ExceptionType}: {Message}",
                exception.GetType().Name,
                exception.Message);

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ApiError("База данных недоступна."));
        }
    }
}
