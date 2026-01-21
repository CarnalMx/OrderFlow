using OrderFlow.Application.Outbox.Handlers;
using OrderFlow.Domain.Models;

namespace OrderFlow.Tests.Outbox;

public class OrderConfirmedHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidPayload_DoesNotThrow()
    {
        var handler = new OrderConfirmedHandler();

        var msg = new OutboxMessage
        {
            Type = "OrderConfirmed",
            PayloadJson = "{\"orderId\":123}"
        };

        await handler.HandleAsync(msg, CancellationToken.None);
    }
}
