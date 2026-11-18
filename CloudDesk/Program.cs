using CloudDesk;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.RequestQuery
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});
builder.Logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);

builder.Services.AddSingleton<NpgsqlConnectionFactory>();
builder.Services.AddSingleton<TicketStore>();

var app = builder.Build();

app.UseHttpLogging();
app.UseMiddleware<DatabaseFailureHandler>();

app.MapGet("/", GetRoot.Handle);
app.MapGet("/health", GetHealth.Handle);
app.MapGet("/api/stats", GetStats.Handle);
app.MapGet("/api/tickets", ListTickets.Handle);
app.MapGet("/api/tickets/{id:int}", GetTicket.Handle);
app.MapPost("/api/tickets", CreateTicket.Handle);
app.MapPut("/api/tickets/{id:int}", UpdateTicket.Handle);
app.MapDelete("/api/tickets/{id:int}", DeleteTicket.Handle);

app.Run();
