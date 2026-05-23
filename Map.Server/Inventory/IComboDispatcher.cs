using Map.Server.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// Walks the registered combo set (populated from <c>scripts/combos/</c>
/// at bundle load) and fires each combo's <c>onActive</c> hook against
/// the player's equip-bonus bundle when every member item is equipped.
///
/// <para>
/// Replaces the old <c>IItemCombosService</c> + <c>ActiveCombo</c> chain
/// that ran combo scripts as strings through the runtime DSL bridge.
/// CONV-3 moves combo dispatch onto the shared <c>mmo-scripts</c> V8
/// engine — the same engine that hosts NPCs / shops / items.
/// </para>
/// </summary>
public interface IComboDispatcher
{
    /// <summary>
    /// For each combo whose member set is a subset of <paramref name="equipped"/>,
    /// invoke its <c>onActive</c> hook with a context wrapping
    /// <paramref name="bundle"/> + <paramref name="player"/>. Bonuses
    /// accumulate into the bundle in-place.
    /// </summary>
    void ApplyActiveCombos(
        IReadOnlyList<InventoryItem> equipped,
        EquipBonusBundle bundle,
        PlayerEntity player);
}
