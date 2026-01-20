using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Data;
using OrderFlow.Api.Dtos;
using OrderFlow.Api.Models;

namespace OrderFlow.Api.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapGet("/orders", async (AppDbContext db) =>
        {
            var orders = await db.Orders
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return Results.Ok(orders);
        });

        app.MapGet("/orders/{id:int}", async (int id, AppDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapPost("/orders", async (CreateOrderRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return Results.BadRequest(new { error = "CustomerName is required" });

            var order = new Order
            {
                CustomerName = request.CustomerName.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return Results.Created($"/orders/{order.Id}", order);
        });
    }
}
