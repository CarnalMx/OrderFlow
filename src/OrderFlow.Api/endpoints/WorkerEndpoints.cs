using OrderFlow.Application.Abstractions;

namespace OrderFlow.Api.Endpoints;

public static class WorkerEndpoints
{
    public static void MapWorkerEndpoints(this WebApplication app)
    {
        app.MapGet("/worker/status", async (HttpContext ctx, IWorkerMetrics metrics, IOutboxReader outbox) =>
        {
            var snap = metrics.GetSnapshot();

            var pending = await outbox.CountPendingAsync(DateTime.UtcNow, ctx.RequestAborted);

            return Results.Ok(new
            {
                running = snap.Running,
                processedMessages = snap.ProcessedMessages,
                avgProcessedTimeMs = snap.AvgProcessedTimeMs,
                queuePending = pending
            });
        });
    }
}