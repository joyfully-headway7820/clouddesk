namespace CloudDesk;

public static class GetStats
{
    public static async Task<IResult> Handle(TicketStore store, CancellationToken cancellationToken)
    {
        var stats = await store.GetStatsAsync(cancellationToken);
        return Results.Ok(stats);
    }
}
