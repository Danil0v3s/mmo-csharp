using Map.Server.Entities;

namespace Map.Server.Inventory.ItemEffects;

/// <summary>
/// One item's "what happens when used" — the strategy in our
/// strategy-pattern dispatch table. Mirrors the role of an rAthena
/// item_db <c>Script</c> column entry, just hand-coded in C# until the
/// script-engine port is online.
///
/// Strategy-pattern choice: <see cref="IItemUseService"/> resolves the
/// item row → looks up the handler by aegis name in
/// <see cref="ItemEffectRegistry"/> → calls <see cref="Apply"/>. New
/// items add a class implementing this interface; no switch case to
/// edit. Matches the shape of <c>SkillUnitService.SpecFor</c>.
/// </summary>
public interface IItemEffectHandler
{
    /// <summary>Aegis name as it appears in <c>item_db.NameAegis</c>.</summary>
    string AegisName { get; }

    /// <summary>Apply the effect to <paramref name="user"/>. Returns true on success.</summary>
    bool Apply(PlayerEntity user);
}
