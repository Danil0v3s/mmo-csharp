using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// rAthena <c>skill_addtimerskill</c> (skill.cpp ~660) — schedules a
/// per-skill callback to fire after a delay. Multi-hit skills (Sonic
/// Blow's 8-hit chain, Storm Gust's 3 staggered hits, Adoramus's
/// delayed-strike, Tetra Vortex's 4 elemental bursts) use this to
/// stagger their resolution across ticks.
///
/// <para>rAthena keeps the deferred state on the unit's
/// <c>skilltimerskill[MAX_SKILLTIMERSKILL]</c> slot array and the
/// <c>skill_timerskill</c> function is a giant switch over skill ids.
/// The C# port flattens that into a callback: the per-skill body
/// passes a closure that captures the rich context it needs (target
/// id, hit index, level, flag) and the scheduler fires it at the
/// target tick. This eliminates the central switch and keeps every
/// skill's deferred logic colocated with its other hooks.</para>
///
/// <para>Cancellation: rAthena clears the slot when the unit dies or
/// status-cures away the cast. The C# variant matches by checking
/// the source / target are still in the registry at fire time
/// (mirrors <c>src-&gt;prev == nullptr</c> / <c>status_isdead</c>
/// guards inside <c>skill_timerskill</c>).</para>
/// </summary>
public interface ISkillTimerService
{
    /// <summary>
    /// Schedule a delegate to fire after <paramref name="delayMs"/>.
    /// At fire time, source and target are re-resolved from the
    /// registry; if either is gone or on a different map, the
    /// callback is silently dropped (rAthena <c>break</c> branches
    /// inside <c>skill_timerskill</c>).
    /// </summary>
    /// <param name="src">Caster.</param>
    /// <param name="target">Target. May be the source for self-targeted
    /// deferred actions (Storm Gust's central tick, Falcon Assault
    /// post-hit).</param>
    /// <param name="delayMs">Milliseconds until the callback fires.</param>
    /// <param name="skillId">Skill id this timer belongs to (for
    /// diagnostics / future cancellation by skill id).</param>
    /// <param name="skillLevel">Skill level passed back to the callback.</param>
    /// <param name="callback">The deferred action. Receives the freshly
    /// resolved entities + level. May read context state captured at
    /// schedule time (rAthena passes the <c>type</c>+<c>flag</c> ints
    /// for hit-index / sub-mode — closures replace both).</param>
    void Schedule(Entity src, Entity target, int delayMs, ushort skillId, ushort skillLevel,
        Action<Entity, Entity, ushort> callback);

    /// <summary>
    /// Drain due entries. Called once per game tick by the same loop
    /// driver that ticks <c>IDelayedDamageService</c>.
    /// </summary>
    void Tick(long nowTick);
}
