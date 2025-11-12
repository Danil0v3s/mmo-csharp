using Core.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Database.Seeds;

/// <summary>
/// Seeds the database with initial data from SQL script files.
/// This runs once on application startup if the database is empty.
/// </summary>
public class DatabaseSeeder
{
    private readonly GameDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(GameDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database if it's empty. Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if database is already seeded
            if (await IsDatabaseSeededAsync(ct))
            {
                _logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            _logger.LogInformation("Starting database seeding from SQL scripts...");

            await _context.Database.ExecuteSqlRawAsync("START TRANSACTION;", ct);

            await ExecuteSqlScriptAsync("Scripts/seed_initial_data.sql", ct);
            await ExecuteSqlScriptAsync("Scripts/seed_roulette_default_data.sql", ct);
            await ExecuteSqlScriptAsync("Scripts/seed_item_db_equip.sql", ct);
            await ExecuteSqlScriptAsync("Scripts/seed_item_db_etc.sql", ct);
            await ExecuteSqlScriptAsync("Scripts/seed_item_db_usable.sql", ct);
            await ExecuteSqlScriptAsync("Scripts/seed_mob_db.sql", ct);

            await _context.Database.ExecuteSqlRawAsync("COMMIT", ct);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            await _context.Database.ExecuteSqlRawAsync("ROLLBACK", ct);
            _logger.LogError(ex, "Failed to seed database");
            throw;
        }
    }

    /// <summary>
    /// Checks if the database already has seed data.
    /// </summary>
    private async Task<bool> IsDatabaseSeededAsync(CancellationToken ct)
    {
        // Check if clans exist - these are part of initial seed data
        return await _context.Clans.AnyAsync(ct);
    }

    /// <summary>
    /// Executes a SQL script file.
    /// </summary>
    private async Task ExecuteSqlScriptAsync(string relativePath, CancellationToken ct)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var scriptPath = Path.Combine(baseDirectory, relativePath);

        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("SQL script not found: {ScriptPath}", scriptPath);
            return;
        }

        _logger.LogInformation("Executing SQL script: {ScriptPath}", scriptPath);

        var sql = await File.ReadAllLinesAsync(scriptPath, ct);
        foreach (var statement in sql)
        {
            var trimmedStatement = statement.Trim();

            // Skip empty lines, comments, and section headers
            if (string.IsNullOrWhiteSpace(trimmedStatement) || trimmedStatement.StartsWith("--") || trimmedStatement.StartsWith("/*"))
            {
                continue;
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(trimmedStatement, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute statement: {Statement}", trimmedStatement);
                throw;
            }
        }
    }
}