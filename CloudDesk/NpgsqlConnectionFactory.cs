using Npgsql;

namespace CloudDesk;

public sealed class NpgsqlConnectionFactory
{
    private readonly string? _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = _connectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Default.");

        try
        {
            return await ConnectAsync(connectionString, cancellationToken);
        }
        catch (NpgsqlException)
        {
            NpgsqlConnection.ClearAllPools();
            return await ConnectAsync(connectionString, cancellationToken);
        }
    }

    private static async Task<NpgsqlConnection> ConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
