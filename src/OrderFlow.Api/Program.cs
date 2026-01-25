using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Api.Endpoints;
using OrderFlow.Application.Services;
using OrderFlow.Application.Abstractions;
using OrderFlow.Infrastructure.Repositories;
using OrderFlow.Api.Middlewares;
using OrderFlow.Api.BackgroundServices;
using OrderFlow.Application.Outbox;
using OrderFlow.Application.Outbox.Handlers;
using OrderFlow.Api.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOutboxStore, OutboxStore>();
builder.Services.AddScoped<IOutboxReader, OutboxReader>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

builder.Services.AddScoped<IOutboxHandler, OrderConfirmedHandler>();

builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddSingleton<IWorkerMetrics, InMemoryWorkerMetrics>();
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ui", p =>
    p.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<OrderService>();
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCors("ui");
// For testing purposes only

if (app.Environment.IsDevelopment())
{

    app.MapGet("/debug/crash", () =>
    {
        throw new Exception("Boom");
    })
    .ExcludeFromDescription();


}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapOrdersEndpoints();
app.MapOutboxEndpoints();
app.MapWorkerEndpoints();

app.Run();

public partial class Program { }
