using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace CloudDesk;

public sealed class TicketStore
{
    private const string TicketColumns = "id, title, description, priority, status, created_at";

    private readonly NpgsqlConnectionFactory _connections;

    public TicketStore(NpgsqlConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<TicketPage> ListAsync(TicketQuery query, CancellationToken cancellationToken)
    {
        if (query.Q is { Length: > 120 })
            return new TicketPage(0, query.Page, query.PageSize, []);

        var where = BuildWhere(query);
        await using var connection = await _connections.OpenAsync(cancellationToken);

        await using var countCommand = new NpgsqlCommand($"SELECT COUNT(*)::int FROM tickets{where}", connection);
        AddFilters(countCommand, query);
        var total = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken),
            CultureInfo.InvariantCulture);

        var direction = query.Sort == "asc" ? "ASC" : "DESC";
        var listSql = $"""
            SELECT {TicketColumns}
            FROM tickets
            {where}
            ORDER BY created_at {direction}
            LIMIT @limit OFFSET @offset
            """;

        await using var listCommand = new NpgsqlCommand(listSql, connection);
        AddFilters(listCommand, query);
        listCommand.Parameters.Add(new NpgsqlParameter("limit", query.PageSize));
        listCommand.Parameters.Add(new NpgsqlParameter("offset", (long)(query.Page - 1) * query.PageSize));

        var items = new List<Ticket>();
        await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadTicket(reader));

        return new TicketPage(total, query.Page, query.PageSize, items);
    }

    public async Task<Ticket?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT {TicketColumns} FROM tickets WHERE id = @id",
            connection);
        command.Parameters.Add(new NpgsqlParameter("id", id));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadTicket(reader);
    }

    public async Task<Ticket> CreateAsync(TicketWrite write, CancellationToken cancellationToken)
    {
        var title = write.Title!.Trim();
        var description = write.Description!.Trim();
        var priority = write.Priority!.Value;
        var status = string.IsNullOrWhiteSpace(write.Status) ? TicketStatus.New : write.Status.Trim();

        await using var connection = await _connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            $"""
            INSERT INTO tickets (title, description, priority, status)
            VALUES (@title, @description, @priority, @status)
            RETURNING {TicketColumns}
            """,
            connection,
            transaction);
        BindWrite(command, title, description, priority, status);

        Ticket ticket;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            ticket = ReadTicket(reader);
        }

        await InsertEventAsync(connection, transaction, ticket.Id, null, ticket.Status, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ticket;
    }

    public async Task<Ticket?> UpdateAsync(int id, TicketWrite write, CancellationToken cancellationToken)
    {
        var title = write.Title!.Trim();
        var description = write.Description!.Trim();
        var priority = write.Priority!.Value;
        var status = write.Status!.Trim();

        await using var connection = await _connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        string? oldStatus;
        await using (var select = new NpgsqlCommand(
            "SELECT status FROM tickets WHERE id = @id FOR UPDATE",
            connection,
            transaction))
        {
            select.Parameters.Add(new NpgsqlParameter("id", id));
            oldStatus = await select.ExecuteScalarAsync(cancellationToken) as string;
        }

        if (oldStatus is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await using var update = new NpgsqlCommand(
            $"""
            UPDATE tickets
            SET title = @title,
                description = @description,
                priority = @priority,
                status = @status
            WHERE id = @id
            RETURNING {TicketColumns}
            """,
            connection,
            transaction);
        update.Parameters.Add(new NpgsqlParameter("id", id));
        BindWrite(update, title, description, priority, status);

        Ticket? ticket;
        await using (var reader = await update.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                ticket = null;
            else
                ticket = ReadTicket(reader);
        }

        if (ticket is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (!string.Equals(oldStatus, status, StringComparison.Ordinal))
            await InsertEventAsync(connection, transaction, id, oldStatus, status, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return ticket;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM tickets WHERE id = @id", connection);
        command.Parameters.Add(new NpgsqlParameter("id", id));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<TicketStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [TicketStatus.New] = 0,
            [TicketStatus.InProgress] = 0,
            [TicketStatus.Done] = 0
        };

        await using var connection = await _connections.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT status, COUNT(*)::int FROM tickets GROUP BY status",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.GetString(0);
            if (counts.ContainsKey(status))
                counts[status] = reader.GetInt32(1);
        }

        return new TicketStats(counts.Values.Sum(), counts);
    }

    private static string BuildWhere(TicketQuery query)
    {
        var conditions = new List<string>();
        if (query.Status is not null)
            conditions.Add("status = @status");
        if (query.Priority is not null)
            conditions.Add("priority = @priority");
        if (query.Q is not null)
            conditions.Add("strpos(lower(title), lower(@q)) > 0");

        return conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", conditions);
    }

    private static void AddFilters(NpgsqlCommand command, TicketQuery query)
    {
        if (query.Status is not null)
            command.Parameters.Add(new NpgsqlParameter("status", query.Status));
        if (query.Priority is int priority)
            command.Parameters.Add(new NpgsqlParameter("priority", priority));
        if (query.Q is not null)
            command.Parameters.Add(new NpgsqlParameter("q", query.Q));
    }

    private static void BindWrite(
        NpgsqlCommand command,
        string title,
        string description,
        int priority,
        string status)
    {
        command.Parameters.Add(new NpgsqlParameter("title", title));
        command.Parameters.Add(new NpgsqlParameter("description", description));
        command.Parameters.Add(new NpgsqlParameter("priority", priority));
        command.Parameters.Add(new NpgsqlParameter("status", status));
    }

    private static async Task InsertEventAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int ticketId,
        string? oldStatus,
        string newStatus,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO ticket_events (ticket_id, old_status, new_status)
            VALUES (@ticket_id, @old_status, @new_status)
            """,
            connection,
            transaction);
        command.Parameters.Add(new NpgsqlParameter("ticket_id", ticketId));
        command.Parameters.Add(new NpgsqlParameter("old_status", NpgsqlDbType.Varchar)
        {
            Value = (object?)oldStatus ?? DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("new_status", newStatus));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Ticket ReadTicket(NpgsqlDataReader reader)
    {
        return new Ticket(
            reader.GetInt32(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("title")),
            reader.GetString(reader.GetOrdinal("description")),
            reader.GetInt32(reader.GetOrdinal("priority")),
            reader.GetString(reader.GetOrdinal("status")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")));
    }
}
