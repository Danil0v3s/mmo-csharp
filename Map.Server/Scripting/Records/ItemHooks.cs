namespace Map.Server.Scripting.Records;

/// <summary>
/// The three lifecycle hooks an item can declare in <c>registerItem({...})</c>.
/// Every field is optional — authors only set the hooks the item needs.
///
/// <list type="bullet">
///   <item><c>OnUse</c> — fires from the CZ_USE_ITEM packet handler when a
///         player consumes the item. Async (the script may suspend on
///         <c>await ctx.player.itemHeal(...)</c> etc.). For equipment items,
///         rAthena historically uses the same Script field for "permanent
///         on-equip bonus" (no on-use action) — those translate to
///         <see cref="OnEquip"/> in our converter, not OnUse.</item>
///   <item><c>OnEquip</c> — fires from the equip handler on success. The
///         host binds an EquipBonusBundle into the dispatch context so
///         <c>ctx.bonus(...)</c> / <c>ctx.bonus2(...)</c> calls accumulate
///         into the player's bundle. Sync (no awaits — equip recalc runs
///         on the game loop and must not suspend).</item>
///   <item><c>OnUnequip</c> — fires from the unequip handler. Same sync
///         contract as OnEquip; usually used to clean up state that
///         OnEquip set up (rare — most items rely on the bundle being
///         rebuilt from scratch on the next recalc).</item>
/// </list>
/// </summary>
public sealed record ItemHooks(
    ScriptHandle? OnUse,
    ScriptHandle? OnEquip,
    ScriptHandle? OnUnequip)
{
    public static readonly ItemHooks Empty = new(null, null, null);

    public bool Any => OnUse != null || OnEquip != null || OnUnequip != null;
}
