using Map.Server.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// DBR-2a: equipment-combo bonus engine. Port of rAthena
/// <c>itemdb_combo_apply</c> / <c>pc_combo</c>
/// (itemdb.cpp + pc.cpp). Each combo fires its rAthena bonus
/// script whenever every member item in the combo is equipped
/// simultaneously; the script un-applies when any member is
/// unequipped.
///
/// <para>
/// Stock rAthena catalog: 7767 combos × 17720 members across
/// <c>item_combo_db</c> + <c>item_combo_member_db</c>. The data path
/// — equipped-set detection — is the critical primitive; actually
/// running the combo's script body lands when the script engine
/// (Jint) is wired into the bonus pipeline.
/// </para>
/// </summary>
public interface IItemCombosService
{
    /// <summary>
    /// Recompute which combos are currently firing for this PC's
    /// equipped item set. Returns the list of (comboId, script) pairs
    /// that just became active or remain active. Called by
    /// <see cref="EquipService.TryRecalcStats"/> after any equip /
    /// unequip / removeoption.
    /// </summary>
    IReadOnlyList<ActiveCombo> RecomputeCombos(MapSessionData session);

    /// <summary>Total combos in the loaded catalog (diagnostics).</summary>
    int CatalogCount { get; }
}

/// <summary>A combo currently firing for a player. <c>Script</c> is the rAthena bonus script body.</summary>
public readonly record struct ActiveCombo(int ComboId, string Script);
