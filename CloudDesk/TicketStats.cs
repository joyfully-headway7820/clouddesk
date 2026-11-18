namespace CloudDesk;

public sealed record TicketStats(int Total, IReadOnlyDictionary<string, int> ByStatus);
