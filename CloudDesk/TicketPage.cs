namespace CloudDesk;

public sealed record TicketPage(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<Ticket> Items);
