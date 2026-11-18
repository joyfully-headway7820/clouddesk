namespace CloudDesk;

public static class DeleteTicket
{
    public static async Task<IResult> Handle(int id, TicketStore store, CancellationToken cancellationToken)
    {
        var deleted = await store.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return Results.NotFound(new ApiError("Заявка не найдена."));

        return Results.NoContent();
    }
}
