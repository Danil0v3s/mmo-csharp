using System.Data;
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
            await EnsureLoginAutoIncrementAsync(ct);

            // Check if database is already seeded
            if (await IsDatabaseSeededAsync(ct))
            {
                _logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            _logger.LogInformation("Starting database seeding from SQL scripts...");

            await _context.Database.ExecuteSqlRawAsync("START TRANSACTION;", ct);

            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_initial_data.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_roulette_default_data.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_db_equip.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_db_etc.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_db_usable.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_mob_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_mob_skill_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_skill_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_abra_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_magicmushroom_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_spellbook_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_quest_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_pet_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_achievement_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_homunculus_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_mercenary_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_instance_db.sql", ct);

            // Second wave of catalogs (Tier 1 finish).
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_castle_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_statpoint.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_exp_homun.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_exp_guild.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_size_fix.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_reputation.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_create_arrow_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_randomopt_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_elemental_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_battleground_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_skill_tree.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_guild_skill_tree.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_mob_summon.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_randomopt_group.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_attr_fix.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_level_penalty.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_job_stats.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_job_exp.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_job_basepoints.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_status_yml.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_combos.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_packages.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_group_db.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_enchant.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_item_reform.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_laphine_synthesis.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_laphine_upgrade.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_refine.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_enchantgrade.sql", ct);
            await ExecuteSqlScriptAsync("Seeds/Scripts/seed_map_drops.sql", ct);

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
        
        // Use raw database connection to avoid EF Core's SQL parsing
        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        
        if (!wasOpen)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
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
                    using var command = connection.CreateCommand();
                    command.CommandText = trimmedStatement;
                    await command.ExecuteNonQueryAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute statement: {Statement}", 
                        trimmedStatement.Length > 500 ? trimmedStatement[..500] + "..." : trimmedStatement);
                    throw;
                }
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Ensures login account IDs follow rAthena baseline (>= 2000000).
    /// Safe to run multiple times.
    /// </summary>
    private async Task EnsureLoginAutoIncrementAsync(CancellationToken ct)
    {
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE `login` AUTO_INCREMENT = 2000000;", ct);
    }
}
