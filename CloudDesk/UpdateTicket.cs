namespace CloudDesk;

public static class UpdateTicket
{
    public static async Task<IResult> Handle(
        int id,
        TicketWrite? body,
        TicketStore store,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return Results.BadRequest(new ApiError("Тело запроса обязательно."));

        var error = TicketValidator.Validate(body, statusRequired: true);
        if (error is not null)
            return Results.BadRequest(new ApiError(error));

        var ticket = await store.UpdateAsync(id, body, cancellationToken);
        if (ticket is null)
            return Results.NotFound(new ApiError("Заявка не найдена."));

        return Results.Ok(ticket);
    }
}
