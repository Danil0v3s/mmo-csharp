namespace Map.Server.Inventory;

/// <summary>
/// rAthena refine system — wraps <c>refine.cpp::status_get_refine_chance</c>
/// + <c>refine_db</c>. Refine groups (Armor / Weapon1..4 / Shadow_Armor /
/// Shadow_Weapon) bucket per equipment class; per-(group, item-level,
/// refine-level) rows carry the stat bonus, and per-attempt rows carry
/// the success rate + zeny cost + material for each ChanceType
/// (Normal, HD, Enriched, Bsb).
///
/// DBR-2c: sources from <see cref="Core.Database.Repositories.Api.IRefineDbRepository"/>
/// (4 groups / 160 level rows / 390 chance rows seeded by DB-8h).
/// </summary>
public interface IRefineService
{
    /// <summary>True if the catalog has at least one group loaded.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Bonus stat conferred at this refine level for this (group, itemLevel)
    /// pair. Returns 0 when the row is missing — refine of an unmodeled
    /// item simply confers no bonus.
    /// </summary>
    int GetRefineBonus(string groupName, int itemLevel, int refineLevel);

    /// <summary>
    /// Per-attempt success rate (Rate / 10000), zeny price, and material
    /// Aegis name for an upgrade attempt. Returns null when no row
    /// matches — caller treats as "refine impossible at this level".
    /// </summary>
    RefineAttempt? GetRefineChance(string groupName, int itemLevel, int refineLevel, string chanceType);

    /// <summary>Rebuild the in-memory cache from the SQL catalog.</summary>
    void Reload();
}

/// <summary>Outcome row for an upgrade attempt — see <see cref="IRefineService.GetRefineChance"/>.</summary>
public sealed record RefineAttempt(int Rate, int Price, string MaterialAegis);
