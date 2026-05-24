using Map.Server.Session;

namespace Map.Server.Inventory;

/// <summary>
/// Mediates the equip / unequip wear-state on a session's inventory.
/// Port of rAthena <c>pc_equipitem</c> / <c>pc_unequipitem</c>
/// (pc.cpp:11864 / pc.cpp:12238) — chooses the actual slot bits,
/// drops anything currently in those slots, mutates
/// <see cref="InventoryItem.Equip"/>, and triggers a stat recalc.
///
/// <para>
/// This slice handles weapon (right-hand), shield (left-hand),
/// armor, helms (top/mid/low), garment, shoes, accessories (L/R),
/// and ammo. Costume slots and shadow gear are deferred —
/// behavior matches rAthena for the gear catalog we currently load.
/// </para>
/// </summary>
public interface IEquipService
{
    /// <summary>
    /// Wear the item at <paramref name="serverIndex"/> into the slots
    /// requested by <paramref name="requestedPos"/>. Any item already
    /// occupying the resolved slots is unequipped first. Returns the
    /// outcome + the slot bitmask actually applied (0 on failure).
    /// </summary>
    EquipOpResult Equip(MapSessionData session, int serverIndex, uint requestedPos, out uint appliedPos);

    /// <summary>
    /// Remove the item at <paramref name="serverIndex"/> from its
    /// currently-worn slots. Returns the outcome + the bitmask the
    /// item was wearing (0 on failure).
    /// </summary>
    EquipOpResult Unequip(MapSessionData session, int serverIndex, out uint clearedPos);

    /// <summary>
    /// rAthena <c>pc_checkequip</c> (pc.cpp:6193) — return the
    /// inventory index of the item equipped in the slots specified
    /// by <paramref name="equipPos"/>, or -1 if nothing is equipped
    /// there. Used by every skill formula that branches on equipped
    /// gear ("does this character have a shield?", "is the right
    /// hand a bow?", etc.). The bit positions match
    /// <see cref="EquipBits"/>.
    /// </summary>
    int CheckEquip(MapSessionData session, uint equipPos);

    /// <summary>
    /// Convenience overload — looks up the equipped row directly
    /// rather than the inventory index. Returns null if nothing is
    /// equipped at the requested slot.
    /// </summary>
    InventoryItem? FindEquipped(MapSessionData session, uint equipPos);
}

/// <summary>
/// Outcome of an equip / unequip attempt. Maps to the
/// <c>clif_equipitemack_flag</c> wire code on the ack.
/// </summary>
public enum EquipOpResult
{
    Ok = 0,
    Fail = 1,
    InvalidSlot = 2,
    NotIdentified = 3,
    NotEquippable = 4,
    NoSuchItem = 5,
    PlayerDead = 6,
}
