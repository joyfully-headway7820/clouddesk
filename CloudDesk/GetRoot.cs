namespace CloudDesk;

public static class GetRoot
{
    public static IResult Handle() =>
        Results.Ok(new ServiceIndex("CloudDesk", "/health", "/api/tickets", "/api/stats"));
}
