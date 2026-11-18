namespace CloudDesk;

public static class TicketStatus
{
    public const string New = "new";
    public const string InProgress = "in_progress";
    public const string Done = "done";

    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        New,
        InProgress,
        Done
    };

    public static bool IsAllowed(string status) => Allowed.Contains(status);
}
