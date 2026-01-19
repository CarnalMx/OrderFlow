using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Data;
using OrderFlow.Api.Models;
using OrderFlow.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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


app.Run();
