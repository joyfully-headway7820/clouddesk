namespace CloudDesk;

public sealed record TicketQueryResult(TicketQuery? Query, string? Error)
{
    public static TicketQueryResult Ok(TicketQuery query) => new(query, null);

    public static TicketQueryResult Fail(string error) => new(null, error);
}
