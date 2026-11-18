namespace CloudDesk;

public static class ListTickets
{
    public static async Task<IResult> Handle(
        HttpRequest request,
        TicketStore store,
        CancellationToken cancellationToken)
    {
        var parsed = ParseTicketQuery.Parse(request.Query);
        if (parsed.Query is not TicketQuery query)
            return Results.BadRequest(new ApiError(parsed.Error ?? "Некорректный запрос."));

        var page = await store.ListAsync(query, cancellationToken);
        return Results.Ok(page);
    }
}
