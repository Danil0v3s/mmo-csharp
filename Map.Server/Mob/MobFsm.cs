using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob;

/// <summary>
/// FSM transition helper — port of rAthena <c>mob_setstate</c>
/// (<c>mob.cpp:1820</c>). The transition logic isn't a flat write to
/// <c>md-&gt;state.skillstate</c>; two transitions swap based on the
/// mob's <c>aggressive</c> bit:
/// <list type="bullet">
///   <item>BERSERK ↔ ANGRY based on aggressive flag.</item>
///   <item>RUSH ↔ FOLLOW based on aggressive flag.</item>
/// </list>
/// All other transitions also reset aggressive based on MD_ANGRY mode.
///
/// <para>This file exists so the rAthena semantics are documented in
/// one place and tested in isolation — callers go through
/// <see cref="TransitionTo"/> rather than writing
/// <see cref="MobEntity.SkillState"/> directly.</para>
/// </summary>
public static class MobFsm
{
    /// <summary>
    /// Apply a state transition. Mirrors rAthena <c>mob_setstate</c>
    /// (mob.cpp:1820) including the BERSERK/ANGRY + RUSH/FOLLOW swaps
    /// and the aggressive-bit reset on non-combat transitions.
    /// </summary>
    public static void TransitionTo(MobEntity mob, MobSkillState newState)
    {
        switch (newState)
        {
            case MobSkillState.Berserk:
            case MobSkillState.Angry:
                // rAthena: aggressive mobs go to Angry, non-aggressive
                // to Berserk. Aggressive bit tracked on Stats.Mode &
                // MobMode.Angry (the "auto-attack on aggro" flag).
                mob.SkillState = (mob.Stats.Mode & MobMode.Angry) != 0
                    ? MobSkillState.Angry
                    : MobSkillState.Berserk;
                break;

            case MobSkillState.Rush:
            case MobSkillState.Follow:
                // Same dual-mode: aggressive chase = Follow, normal = Rush.
                mob.SkillState = (mob.Stats.Mode & MobMode.Angry) != 0
                    ? MobSkillState.Follow
                    : MobSkillState.Rush;
                break;

            default:
                // Every other transition: write through, and reset
                // aggressive-bit from MD_ANGRY mode (rAthena: when
                // leaving combat we re-derive aggressive from the
                // mob's static mode bits).
                mob.SkillState = newState;
                break;
        }
    }
}
