namespace Map.Server.Inventory;

/// <summary>
/// DBR-2f: Lapine (Laphine) synthesis + upgrade pipelines. Port of
/// rAthena <c>laphine_synthesis</c> + <c>laphine_upgrade</c>
/// (db/re/laphine_synthesis.yml + laphine_upgrade.yml).
///
/// <para>
/// Lapine Synthesis: opener consumes N matching ingredient items,
/// produces one of a reward group's items. Lapine Upgrade: opener
/// consumes a target item, produces the same item with refine /
/// random-option enchantment.
/// </para>
/// </summary>
public interface ILaphineService
{
    /// <summary>
    /// Synthesis recipe for an opener item. Null if not a synthesis
    /// trigger. <c>RewardGroup</c> points at an item-group catalog
    /// entry (see IItemGroupService for the roll); requirements list
    /// items the player must consume.
    /// </summary>
    LaphineSynthesisConfig? GetSynthesis(string openerItemAegis);

    /// <summary>
    /// Upgrade recipe for an opener item. Null if not an upgrade
    /// trigger. <c>Targets</c> lists items the upgrade can apply to.
    /// </summary>
    LaphineUpgradeConfig? GetUpgrade(string openerItemAegis);

    /// <summary>Diagnostics: synthesis + upgrade catalog sizes.</summary>
    (int Synthesis, int Upgrade) CatalogCount { get; }
}

public sealed record LaphineSynthesisConfig(
    string OpenerAegis,
    string? RewardGroup,
    int RequiredRequirementsCount,
    IReadOnlyList<LaphineRequirement> Requirements);

public sealed record LaphineRequirement(
    string ItemAegis,
    int? RefineMin,
    int? RefineMax);

public sealed record LaphineUpgradeConfig(
    string OpenerAegis,
    int? MinimumRefine,
    IReadOnlyList<string> TargetItems);
