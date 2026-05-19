using Map.Server.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// Companion helpers to <see cref="IEquipService"/> that mirror the
/// remaining rAthena <c>pc_*</c> equipment functions. Kept as a
/// separate interface so the core equip/unequip API stays narrow.
///
/// Where the underlying data isn't ready yet, the implementation
/// returns a documented "no-op for now" — but the canonical entry
/// point exists so callers (scripts, atcommands, item-effects) can
/// be ported without grep-rewriting.
/// </summary>
public interface IPlayerEquipHelpers
{
    /// <summary>
    /// rAthena <c>pc_calcweapontype</c> (pc.cpp:923) — derives the
    /// composite weapon type from the two weapon slots (single, dual,
    /// shield-only, etc.) and stores it for the skill-cast WeaponType
    /// mask. First slice reads the primary weapon's type only.
    /// </summary>
    void CalcWeaponType(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_equiplookall</c> (pc.cpp:6203) — re-broadcasts
    /// every equipped slot via <c>clif_changelook</c>. Used by
    /// pc_setpos / pc_jobchange / disguise toggles to ensure newly-
    /// visible viewers see the gear.
    /// </summary>
    void EquipLookAll(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_equipswitch_remove</c> (pc.cpp:13288) — drop
    /// an item from the equipswitch (alternate equip set) table.
    /// First slice clears the EquipSwitch flag on the inventory row;
    /// full second-set swap on hotkey lands later.
    /// </summary>
    void EquipSwitchRemove(PlayerEntity pc, int inventoryIndex);

    /// <summary>
    /// rAthena <c>pc_set_costume_view</c> (pc.cpp:11796) — picks
    /// which costume layer shows on the head bone (costume bypasses
    /// item-stats but renders the sprite). Stub until costume-stat
    /// suppression lands in <see cref="EquipBonusAggregator"/>.
    /// </summary>
    void SetCostumeView(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_insert_card</c> (pc.cpp:5151) — slot a card into
    /// an equipped item's card slots. Stub until the card-slot
    /// inventory column lands.
    /// </summary>
    bool InsertCard(PlayerEntity pc, int cardInventoryIndex, int equipInventoryIndex);

    /// <summary>
    /// rAthena <c>pc_check_expiration</c> / <c>pc_expire_check</c>
    /// (pc.cpp:445 / 12923) — sweeps the inventory + char expiration
    /// fields and removes anything past its lifetime. Run periodically
    /// from the autosave loop.
    /// </summary>
    void CheckExpiration(PlayerEntity pc);
}
