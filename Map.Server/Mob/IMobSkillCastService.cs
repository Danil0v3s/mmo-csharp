using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Canonical entry point for rAthena <c>mobskill_use</c>
/// (<c>mob.cpp:4275-4502</c>) — the loop that walks
/// <see cref="Mob.MobDbEntry.Skills"/>, applies state + condition +
/// permillage gates, picks a target via the MST_* enum, and dispatches
/// to <see cref="Skills.ISkillCastService.StartCast"/>.
///
/// <para>Split out from <see cref="IMobAiService"/> so the picker can
/// be tested in isolation against the T4.2 condition table, and so
/// future <see cref="Mob.IMobAiService.NotifyAttacked"/> /
/// mob_skill_event paths reuse the same logic.</para>
///
/// <para>Mirrors rAthena <c>bool mobskill_use(mob_data *md, t_tick tick,
/// int32 event, int64 damage)</c> — see <c>mob.cpp:4275</c>.</para>
/// </summary>
public interface IMobSkillCastService
{
    /// <summary>
    /// Walk the mob's skill list and fire the first row whose gates
    /// pass. Returns true when a skill was cast (caller should usually
    /// skip the basic-swing for this tick).
    /// </summary>
    /// <param name="mob">The mob doing the thinking.</param>
    /// <param name="nowTick">Current game tick (ms since boot).</param>
    /// <param name="event">
    /// rAthena <c>event</c> arg. <c>-1</c> = passive "idle think" tick;
    /// any other value = a specific MSC_* trigger (event-driven from
    /// <c>mobskill_event</c>).
    /// </param>
    /// <param name="damage">
    /// Damage payload for event-driven calls — used by MSC_DAMAGEDGT.
    /// Zero for idle ticks.
    /// </param>
    bool TryUseSkill(MobEntity mob, long nowTick, int @event = -1, int damage = 0);

    /// <summary>
    /// rAthena <c>mobskill_event(md, src, tick, flag, damage)</c>
    /// (<c>mob.cpp:4506</c>) — dispatch a one-shot trigger
    /// (CloseAttacked, LongRangeAttacked, SkillUsed, GroundAttacked,
    /// CastTargeted). Threads the source-of-event and damage into the
    /// picker so cond1 = matching MSC fires immediately.
    /// </summary>
    bool NotifyEvent(MobEntity mob, Entity? src, long nowTick, MobSkillCondition trigger, int damage = 0, ushort triggerSkillId = 0);
}
