using OrderFlow.Application.Abstractions;

namespace OrderFlow.Api.Endpoints;

public static class OutboxEndpoints
{
    public static void MapOutboxEndpoints(this WebApplication app)
    {
        app.MapGet("/outbox", async (HttpContext ctx, IOutboxReader outbox, string? status, int? take) =>
        {
            var result = await outbox.GetAsync(
                status: status,
                take: take ?? 50,
                ct: ctx.RequestAborted);

            return Results.Ok(result);
        });

        app.MapGet("/outbox/{id:long}", async (HttpContext ctx, IOutboxReader outbox, long id) =>
        {
            var msg = await outbox.GetByIdAsync(id, ctx.RequestAborted);
            return msg is null ? Results.NotFound() : Results.Ok(msg);
        });
    }
}