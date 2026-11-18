namespace CloudDesk;

public static class CreateTicket
{
    public static async Task<IResult> Handle(
        TicketWrite? body,
        TicketStore store,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return Results.BadRequest(new ApiError("Тело запроса обязательно."));

        var error = TicketValidator.Validate(body, statusRequired: false);
        if (error is not null)
            return Results.BadRequest(new ApiError(error));

        var ticket = await store.CreateAsync(body, cancellationToken);
        return Results.Created($"/api/tickets/{ticket.Id}", ticket);
    }
}
