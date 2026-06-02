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
    /// <param name="hits">COMBAT-17 — displayed hit count (rAthena div_) for
    /// the <c>ZC_NOTIFY_ACT3</c> packet. Skill callers pass the skill's hit
    /// count (Sonic Blow = 8); <paramref name="damage"/> stays the resolved
    /// total. Default 1 (single hit).</param>
    /// <returns>The actual damage applied (clamped to target's remaining HP).</returns>
    int ApplyDamage(Entity target, int damage, Entity? source = null, int hits = 1);

    /// <summary>
    /// Resolve a full melee swing (calc + apply). Calls
    /// <see cref="IBattleCalculator.CalcWeaponAttack"/> and emits the
    /// damage with the right action type so the client renders the right
    /// hit/miss/crit animation. Returns the resolved <see cref="BattleDamage"/>
    /// so the caller (auto-attack loop / GM commands) can react to it.
    /// </summary>
    BattleDamage PerformMeleeAttack(Entity source, Entity target);

    /// <summary>
    /// COMBAT-47 — a Safety Wall cell blocks a melee hit (consuming one block); a
    /// Pneuma cell blocks a ranged hit. The skill damage funnel calls this so a
    /// melee/ranged SKILL is intercepted like the auto-attack. Returns true when
    /// blocked. Default false for test doubles that don't model ground units.
    /// </summary>
    bool TryGroundUnitBlock(Entity target, bool isShortRange) => false;
}
