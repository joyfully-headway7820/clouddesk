namespace CloudDesk;

public sealed record TicketQuery(
    string? Status,
    int? Priority,
    string? Q,
    string Sort,
    int Page,
    int PageSize);
