namespace CloudDesk;

public sealed record Ticket(
    int Id,
    string Title,
    string Description,
    int Priority,
    string Status,
    DateTimeOffset CreatedAt);
