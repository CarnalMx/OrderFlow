using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // traer 10 mensajes pendientes (no procesados)
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedAtUtc == null)
                    .OrderBy(m => m.Id)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    // Simulación de "procesamiento"
                    _logger.LogInformation("Processing outbox message {Id} {Type}", msg.Id, msg.Type);

                    msg.ProcessedAtUtc = DateTime.UtcNow;
                    msg.AttemptCount += 1;
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessorService error");
            }

            // sleep 1 segundo
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopped");
    }
}
