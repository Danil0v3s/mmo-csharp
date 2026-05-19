using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Drives mob "hard AI" — target acquisition, chase, attack, lose-target.
/// Port of rAthena <c>mob_ai_hard</c> / <c>mob_ai_sub_hard</c>
/// (mob.cpp:1737/1741), first slice:
///
/// <list type="number">
///   <item>Throttle by MIN_MOBTHINKTIME (rAthena default 100 ms).</item>
///   <item>If <c>target_id</c> set, validate it's alive + on map.</item>
///   <item>If still no target and mob is <see cref="Status.MobMode.Aggressive"/>,
///         scan PCs within <c>db.range2</c> (view range) and lock the closest.</item>
///   <item>Hand off to <see cref="Combat.IAttackService.StartAttack"/> with
///         <c>continuous = true</c>; chase + swing cadence land there.</item>
/// </list>
///
/// Non-aggressive modes (Looter, Assist, Slave, etc.) plug in here as
/// they port.
/// </summary>
public interface IMobAiService
{
    /// <summary>Process one game-tick across every live mob.</summary>
    void Tick(long nowTick);

    /// <summary>
    /// Called from <see cref="Combat.IDamageService.ApplyDamage"/> when a
    /// mob takes damage from a known source. rAthena <c>mob_damage</c>
    /// path (mob.cpp:1743): if the attacker is out of melee reach, the
    /// mob increments <c>md->state.attacked_count</c>. When the counter
    /// crosses <c>battle.mob_rudeattacked_count</c> the AI runs
    /// MSC_RUDEATTACKED — if no skill matches it falls back to
    /// <c>unit_escape</c> (walk away from the attacker).
    /// </summary>
    void NotifyAttacked(MobEntity mob, Entity attacker);
}
