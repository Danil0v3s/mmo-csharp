using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Entry point for "apply this much damage to that entity." rAthena's
/// <c>battle_check_target</c> + <c>battle_calc_attack</c> + <c>status_damage</c>
/// chain collapsed into one façade so MS3 combat can plug in formula work
/// piecemeal without touching every caller (GM <c>@damage</c>, eventually
/// the auto-attack loop, skill effects, MVP damage, etc.).
///
/// MS3 first slice: just the HP-mutation + visibility broadcast pipeline.
/// Formula stage (ATK, defense, hit/flee, elemental modifiers) is owned by
/// later commits.
/// </summary>
public interface IDamageService
{
    /// <summary>
    /// Apply <paramref name="damage"/> HP to <paramref name="target"/>, attributed
    /// to <paramref name="source"/> (may be null for environmental damage).
    /// Broadcasts <c>ZC_NOTIFY_ACT3</c> to AOI viewers regardless of outcome;
    /// when HP reaches 0 the target's death pipeline fires (mob: drop loot +
    /// schedule respawn; PC death is stubbed until MS3 savepoints land).
    /// </summary>
    /// <returns>The actual damage applied (clamped to target's remaining HP).</returns>
    int ApplyDamage(Entity target, int damage, Entity? source = null);
}
