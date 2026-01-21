using System.Text.Json;
using OrderFlow.Application.Outbox;
using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Outbox.Handlers;

public class OrderConfirmedHandler : IOutboxHandler
{
    public string Type => "OrderConfirmed";

    public Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        // Ejemplo simple: parsear el payload
        var payload = JsonSerializer.Deserialize<OrderConfirmedPayload>(message.PayloadJson);

        Console.WriteLine($"[HANDLER] OrderConfirmed received. OrderId={payload?.OrderId}");

        return Task.CompletedTask;
    }

    private sealed class OrderConfirmedPayload
    {
        public int OrderId { get; set; }
    }
}