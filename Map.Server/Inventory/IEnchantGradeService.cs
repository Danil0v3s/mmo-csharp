namespace Map.Server.Inventory;

/// <summary>
/// rAthena enchantgrade system — wraps <c>enchantgrade.cpp</c> +
/// <c>enchantgrade_db</c>. Each (EquipType, ItemLevel, Grade) is a
/// row in the catalog; per-refine attempts have a Chance / 10000
/// in <see cref="Core.Database.Entities.EnchantGradeChanceDbEntity"/>.
///
/// DBR-2d: sources from
/// <see cref="Core.Database.Repositories.Api.IEnchantGradeDbRepository"/>.
/// </summary>
public interface IEnchantGradeService
{
    /// <summary>True if at least one chance row was loaded.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Success chance / 10000 for an enchant-grade upgrade attempt
    /// at <paramref name="refine"/> for a (<paramref name="equipType"/>,
    /// <paramref name="itemLevel"/>, <paramref name="grade"/>) row.
    /// Returns 0 if the catalog has no matching row.
    /// </summary>
    int GetUpgradeChance(string equipType, int itemLevel, string grade, int refine);

    /// <summary>Rebuild the in-memory cache from the SQL catalog.</summary>
    void Reload();
}
