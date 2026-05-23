using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Renewal elemental attribute multiplier table from
/// <c>rathena/db/re/attr_fix.yml</c> (4 levels × 10 attacker × 10
/// defender = 400 rows). Multipliers are %% values (100 = 1.0×).
///
/// DBR-1a: the 240-line baked table that used to live here has been
/// retired in favour of <see cref="IAttrFixDbRepository"/>. The static
/// <see cref="GetRate"/> facade is preserved so existing call sites
/// in <see cref="Map.Server.Combat.BattleCalculator"/> and
/// <see cref="Map.Server.Skills.Resolvers.MagicSkillResolver"/> stay
/// untouched; the boot wiring in <c>Program.cs</c> calls
/// <see cref="Initialize"/> from <see cref="AttrFixCacheService"/> at
/// app start before any combat tick fires. If <see cref="Initialize"/>
/// has not been called (e.g. unit tests that construct
/// <see cref="Map.Server.Combat.BattleCalculator"/> directly), all
/// lookups return 100 (neutral) matching rAthena
/// <c>ATTRIBUTE_DB.getAttribute</c>'s default-on-miss behaviour.
///
/// Renewal only — pre-renewal is permanently out of scope (CLAUDE.md).
/// </summary>
public static class ElementTable
{
    public const int Levels = 4;
    public const int Elements = 10; // Neutral..Undead

    /// <summary>
    /// [Lv-1, atk, def] → %% multiplier. Defaults to all-100 (neutral)
    /// before <see cref="Initialize"/> seeds from the DB so unit tests
    /// that bypass DI get a sane no-op matrix.
    /// </summary>
    private static int[,,] _table = BuildNeutral();

    /// <summary>
    /// Look up the attacker × defender × defense-level multiplier (in %).
    /// Mirrors rAthena <c>elemental_attribute_db.getAttribute</c>
    /// (status.cpp: <c>battle_attr_fix</c> uses this verbatim).
    /// </summary>
    public static int GetRate(BattleElement atk, BattleElement def, int defLevel)
    {
        if (atk < 0 || (int)atk >= Elements) return 100;
        if (def < 0 || (int)def >= Elements) return 100;
        var lvIdx = Math.Clamp(defLevel - 1, 0, Levels - 1);
        return _table[lvIdx, (int)atk, (int)def];
    }

    /// <summary>
    /// Hydrate the matrix from the attr_fix_db rows. Called once at boot
    /// by <see cref="AttrFixCacheService"/>; safe to call again on reload
    /// (full replacement). Unknown element strings or out-of-range levels
    /// are skipped silently — the slot keeps its neutral 100 default.
    /// </summary>
    public static void Initialize(IReadOnlyList<AttrFixDbEntity> rows)
    {
        var fresh = BuildNeutral();
        foreach (var row in rows)
        {
            var lvIdx = row.Level - 1;
            if (lvIdx < 0 || lvIdx >= Levels) continue;
            if (!TryParseElement(row.AttackerElement, out var atk)) continue;
            if (!TryParseElement(row.DefenderElement, out var def)) continue;
            fresh[lvIdx, (int)atk, (int)def] = row.Multiplier;
        }
        _table = fresh;
    }

    /// <summary>
    /// Resolve a rAthena yml element name (Neutral, Water, Earth, Fire,
    /// Wind, Poison, Holy, Dark, Ghost, Undead) to its
    /// <see cref="BattleElement"/> enum value. Case-insensitive.
    /// </summary>
    private static bool TryParseElement(string name, out BattleElement element)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "neutral": element = BattleElement.Neutral; return true;
            case "water":   element = BattleElement.Water;   return true;
            case "earth":   element = BattleElement.Earth;   return true;
            case "fire":    element = BattleElement.Fire;    return true;
            case "wind":    element = BattleElement.Wind;    return true;
            case "poison":  element = BattleElement.Poison;  return true;
            case "holy":    element = BattleElement.Holy;    return true;
            case "dark":    element = BattleElement.Dark;    return true;
            case "ghost":   element = BattleElement.Ghost;   return true;
            case "undead":  element = BattleElement.Undead;  return true;
            default: element = BattleElement.None; return false;
        }
    }

    private static int[,,] BuildNeutral()
    {
        var t = new int[Levels, Elements, Elements];
        for (var l = 0; l < Levels; l++)
            for (var a = 0; a < Elements; a++)
                for (var d = 0; d < Elements; d++)
                    t[l, a, d] = 100;
        return t;
    }
}

/// <summary>
/// DBR-1a load-at-boot hook for <see cref="ElementTable"/>. Constructor
/// runs once during DI bootstrap (singleton) and synchronously seeds the
/// static matrix from <see cref="IAttrFixDbRepository"/> — pattern mirrors
/// the DBR-0 exemplar (BattlegroundService.LoadMapPool). Combat code
/// stays oblivious; just inject a marker dependency to force this
/// service to construct before the first damage tick.
/// </summary>
public sealed class AttrFixCacheService
{
    public AttrFixCacheService(IServiceScopeFactory scopes, ILogger<AttrFixCacheService> logger)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAttrFixDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            ElementTable.Initialize(rows);
            logger.LogInformation("AttrFix matrix hydrated from DB: {Count} rows", rows.Count);
        }
        catch (Exception ex)
        {
            // Fail soft so a DB outage doesn't keep the map server from
            // booting — the neutral fallback matrix (all 100) keeps combat
            // working at parity with "no element fix applied".
            logger.LogError(ex, "AttrFix matrix DB load failed; falling back to neutral matrix");
        }
    }
}
