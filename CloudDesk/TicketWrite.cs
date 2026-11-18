namespace CloudDesk;

public sealed record TicketWrite(
    string? Title,
    string? Description,
    int? Priority,
    string? Status);
