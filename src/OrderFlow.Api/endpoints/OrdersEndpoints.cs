using OrderFlow.Api.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Api.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        // Getters

        app.MapGet("/orders", async (HttpContext ctx, OrderService orders) =>
        {
            var result = await orders.GetAllAsync(ctx.RequestAborted);
            return Results.Ok(result);
        });

        app.MapGet("/orders/{id:int}", async (HttpContext ctx, int id, OrderService orders) =>
        {
            var order = await orders.GetByIdAsync(id, ctx.RequestAborted);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        // Commands

        app.MapPost("/orders", async (HttpContext ctx, CreateOrderRequest request, OrderService orders) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return Results.BadRequest(new { error = "CustomerName is required" });

            var order = await orders.CreateAsync(request.CustomerName, ctx.RequestAborted);
            return Results.Created($"/orders/{order.Id}", order);
        });

        app.MapPost("/orders/{id:int}/confirm", async (HttpContext ctx, int id, OrderService orders) =>
        {
            var (ok, error, order) = await orders.ConfirmAsync(id, ctx.RequestAborted);

            if (order is null) return Results.NotFound();
            if (!ok) return Results.BadRequest(new { error });

            return Results.Ok(order);
        });

        app.MapPost("/orders/{id:int}/cancel", async (HttpContext ctx, int id, OrderService orders) =>
        {
            var (ok, error, order) = await orders.CancelAsync(id, ctx.RequestAborted);

            if (order is null) return Results.NotFound();
            if (!ok) return Results.BadRequest(new { error });

            return Results.Ok(order);
        });

        app.MapPost("/orders/{id:int}/items", async (HttpContext ctx, int id, AddOrderItemRequest request, OrderService orders) =>
        {
            var (ok, error, order) = await orders.AddItemAsync(
                orderId: id,
                name: request.Name,
                quantity: request.Quantity,
                unitPrice: request.UnitPrice,
                ct: ctx.RequestAborted);

            if (order is null) return Results.NotFound();
            if (!ok) return Results.BadRequest(new { error });

            return Results.Ok(order);
        });
    }
}