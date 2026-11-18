using Npgsql;

namespace CloudDesk;

public static class GetHealth
{
    public static async Task<IResult> Handle(
        NpgsqlConnectionFactory connections,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CloudDesk.Health");
        try
        {
            await using var connection = await connections.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return Results.Ok(new HealthReport("ok"));
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Health check failed: {ExceptionType}: {Message}",
                exception.GetType().Name,
                exception.Message);
            return Results.Json(
                new HealthReport("unavailable"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
