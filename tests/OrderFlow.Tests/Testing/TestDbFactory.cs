using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Tests.Testing;

public static class TestDbFactory
{
    public static AppDbContext CreateDbContext()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testing.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("Testing");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, x =>
                x.MigrationsAssembly("OrderFlow.Infrastructure"))
            .Options;

        return new AppDbContext(options);
    }

    public static async Task ResetDatabaseAsync(AppDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}