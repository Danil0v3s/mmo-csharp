using Map.Server.Entities;
using Map.Server.Scripting.Dialog;

namespace Map.Server.Inventory.Script;

/// <summary>
/// The C# object handed to TS items' <c>onUse</c> hooks as <c>ctx</c>.
/// Mirrors the api.d.ts <c>ItemUseContext</c> shape — player + world
/// surfaces plus the consumed item's snapshot and small RNG helpers.
///
/// <para>
/// Reuses the rich <see cref="PlayerContext"/> and <see cref="WorldContext"/>
/// from <c>Map.Server.Scripting.Dialog</c> so item-use scripts can
/// reach the same broad rAthena API surface NPC scripts use (heal /
/// percentHeal / itemHeal, giveExp, perm/account/accountGlobal vars,
/// world.announce, world.getTime, …). The dialog-specific methods
/// (mes / next / select) are technically reachable but never useful
/// from item-use — there's no in-flight dialog session to drive them.
/// </para>
///
/// <para>
/// Wrapping in the shared <c>__invokeHookWithCtx</c> Proxy is what
/// keeps unknown method calls (rAthena builtins we haven't surfaced)
/// from throwing TypeError at runtime — same fail-soft behavior as
/// combo dispatch.
/// </para>
/// </summary>
public sealed class ItemUseHostContext
{
    // Lowercase names so the JS literal matches api.d.ts. ClearScript
    // exposes C# properties verbatim, so PascalCase here would force
    // TS authors to write ctx.Player / ctx.World — wrong shape.
    // ReSharper disable InconsistentNaming
    public PlayerContext player { get; }
    public WorldContext world { get; }
    public ItemUseItemInfo item { get; }
    // ReSharper restore InconsistentNaming

    public ItemUseHostContext(
        PlayerContext player, WorldContext world, ItemUseItemInfo item)
    {
        this.player = player;
        this.world = world;
        this.item = item;
    }

    /// <summary>
    /// <c>rand(max)</c> — uniform [0, max). Matches rAthena's script-side
    /// <c>rand()</c> contract. The host implementation uses
    /// <see cref="Random.Shared"/>; cryptographic randomness isn't a
    /// concern for item-script bonuses.
    /// </summary>
    public int rand(int max) => max <= 0 ? 0 : Random.Shared.Next(max);

    /// <summary>
    /// <c>randRange(min, max)</c> — uniform [min, max] inclusive on both
    /// ends to mirror rAthena's <c>rand(min,max)</c>.
    /// </summary>
    public int randRange(int min, int max)
    {
        if (max < min) (min, max) = (max, min);
        return Random.Shared.Next(min, max + 1);
    }
}

/// <summary>
/// Snapshot of the item triggering an <c>onUse</c> hook. Lowercase
/// property names so the JS literal matches api.d.ts <c>ItemInfo</c>.
/// </summary>
public sealed class ItemUseItemInfo
{
    // ReSharper disable InconsistentNaming
    public int id { get; }
    public string nameAegis { get; }
    public int refine { get; }
    public int slot { get; }
    public int amount { get; }
    // ReSharper restore InconsistentNaming

    public ItemUseItemInfo(int id, string nameAegis, int refine, int slot, int amount)
    {
        this.id = id;
        this.nameAegis = nameAegis;
        this.refine = refine;
        this.slot = slot;
        this.amount = amount;
    }
}
