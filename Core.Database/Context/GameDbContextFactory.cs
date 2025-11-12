using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Core.Database.Context;

/// <summary>
/// Design-time DbContext factory for EF Core migrations.
/// Reads connection string from Core.Database/appsettings.json
/// </summary>
public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args)
    {
        // Load configuration from Core.Database/appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("GameDatabase");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'GameDatabase' not found in Core.Database/appsettings.json. " +
                "Please configure the connection string before running migrations.");
        }

        Console.WriteLine("✓ Using connection string from Core.Database/appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<GameDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

        return new GameDbContext(optionsBuilder.Options);
    }
}

