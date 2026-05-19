using Map.Server.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// Resolves consumable items (potions, scrolls, fly wings, etc.).
/// Port of rAthena <c>pc_useitem</c> (pc.cpp:6329) — validates the
/// slot, evaluates the item's effect, deducts one from the stack,
/// emits the item-use ack packet.
///
/// Effects are encoded in rAthena items via the Script column
/// (Lua-style <c>itemheal …</c> / <c>sc_start …</c> bonuses). We don't
/// have a script parser yet — instead a small hand-built effect
/// table covers the starter potions, with the door open for the
/// script engine to plug in later.
/// </summary>
public interface IItemUseService
{
    /// <summary>
    /// Attempt to use the item in <paramref name="slotIndex"/> of
    /// <paramref name="user"/>'s inventory. Returns true on success
    /// (effect applied, stack decremented).
    /// </summary>
    bool UseItem(PlayerEntity user, int slotIndex);
}
