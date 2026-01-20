using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Data;
using OrderFlow.Api.Dtos;
using OrderFlow.Api.Models;
using OrderFlow.Api.Services;

namespace OrderFlow.Api.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapGet("/orders", async (OrderService orders) =>
        {
            var result = await orders.GetAllAsync();
            return Results.Ok(result);
        });

        app.MapGet("/orders/{id:int}", async (int id, OrderService orders) =>
        {
            var order = await orders.GetByIdAsync(id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapPost("/orders", async (CreateOrderRequest request, OrderService orders) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return Results.BadRequest(new { error = "CustomerName is required" });

            var order = await orders.CreateAsync(request.CustomerName);
            return Results.Created($"/orders/{order.Id}", order);
        });

        app.MapPost("/orders/{id:int}/confirm", async (int id, OrderService orders) =>
        {
            var (ok, error, order) = await orders.ConfirmAsync(id);
            if (!ok) return Results.BadRequest(new { error });

            return Results.Ok(order);
        });

        app.MapPost("/orders/{id:int}/cancel", async (int id, OrderService orders) =>
        {
            var (ok, error, order) = await orders.CancelAsync(id);
            if (!ok) return Results.BadRequest(new { error });

            return Results.Ok(order);
        });


    }
}
