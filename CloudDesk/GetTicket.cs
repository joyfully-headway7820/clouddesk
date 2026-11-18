namespace CloudDesk;

public static class GetTicket
{
    public static async Task<IResult> Handle(int id, TicketStore store, CancellationToken cancellationToken)
    {
        var ticket = await store.GetAsync(id, cancellationToken);
        if (ticket is null)
            return Results.NotFound(new ApiError("Заявка не найдена."));

        return Results.Ok(ticket);
    }
}
