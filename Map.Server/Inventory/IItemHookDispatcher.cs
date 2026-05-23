using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Inventory;

/// <summary>
/// Fires the three item-lifecycle hooks (<c>onUse</c> / <c>onEquip</c> /
/// <c>onUnequip</c>) declared by <c>registerItem({...})</c> calls in the
/// TS bundle. Each <c>Try*</c> returns <c>true</c> when the dispatcher
/// owned the call — that signals the caller to skip its legacy
/// (DSL-bridge / C# handler) fallback so we don't double-apply.
///
/// <para>
/// The dispatcher only fires when a registration exists for the item id;
/// callers that want a no-hook item to still do something fall back to
/// their pre-CONV path (BonusScriptExtractor / ScriptedBonusService for
/// equip bonuses; ItemEffectRegistry for on-use effects).
/// </para>
/// </summary>
public interface IItemHookDispatcher
{
    /// <summary>
    /// Fire the registered <c>onUse</c> hook for <paramref name="itemId"/>.
    /// Returns <c>true</c> if a hook existed and was invoked, <c>false</c>
    /// if no hook is registered (caller falls back to the legacy
    /// <c>ItemEffectRegistry</c> handler).
    /// </summary>
    bool TryInvokeOnUse(MapSessionData session, PlayerEntity player, InventoryItem item);

    /// <summary>
    /// Fire the registered <c>onEquip</c> hook against <paramref name="bundle"/>.
    /// Returns <c>true</c> if a hook existed and was invoked (caller
    /// should skip the DSL Script / EquipScript dispatch for this item
    /// to avoid double-applying bonuses).
    /// </summary>
    bool TryInvokeOnEquip(
        InventoryItem item, EquipBonusBundle bundle, PlayerEntity player,
        IReadOnlyList<InventoryItem> equipped);

    /// <summary>
    /// Fire the registered <c>onUnequip</c> hook. Bonus mutations are
    /// rebuilt from scratch on the next recalc so <c>ctx.bonus(...)</c>
    /// calls here are effectively no-ops; mutations of
    /// <c>ctx.player.*</c> (state changes like HP drain) persist.
    /// Always called fire-and-forget — there's no "skip legacy" branch
    /// because the legacy unequip path doesn't run UnequipScript today.
    /// </summary>
    void TryInvokeOnUnequip(
        InventoryItem item, EquipBonusBundle bundle, PlayerEntity player,
        IReadOnlyList<InventoryItem> equipped);
}
