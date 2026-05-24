using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>mob_can_changetarget</c> + <c>mob_target</c>
/// (mob.cpp:1229-1310). Decides whether a mob is allowed to switch its
/// current <see cref="MobEntity.TargetId"/> to a new attacker, based on
/// the mob's current FSM state and the two MD_CHANGETARGET* mode bits.
///
/// <para>The matrix is:
/// <list type="bullet">
///   <item><b>MSS_BERSERK</b> (engaged melee): only switch if
///   <see cref="Map.Server.Status.MobMode.ChangeTargetMelee"/> is set.</item>
///   <item><b>MSS_RUSH</b> (chasing to engage): only switch if
///   <see cref="Map.Server.Status.MobMode.ChangeTargetChase"/> is set.</item>
///   <item><b>MSS_FOLLOW / ANGRY / IDLE / WALK / LOOT</b>: always allowed.</item>
///   <item>Any other state (Dead, AnyTarget): refused.</item>
/// </list></para>
/// </summary>
public interface IMobChangeTargetService
{
    /// <summary>
    /// rAthena <c>mob_can_changetarget</c> (mob.cpp:1229) — gate check.
    /// Returns true iff the mob may switch from its current target to
    /// <paramref name="newTarget"/> given its current
    /// <see cref="MobEntity.SkillState"/> + mode bits.
    /// </summary>
    bool CanChangeTarget(MobEntity mob, Entity newTarget);

    /// <summary>
    /// rAthena <c>mob_target</c> (mob.cpp:1290) — set the new target.
    /// Returns true on success. If <paramref name="mob"/> already has a
    /// target this calls <see cref="CanChangeTarget"/> as the gate;
    /// when null target → always set unconditionally.
    /// </summary>
    bool TrySetTarget(MobEntity mob, Entity newTarget);

    /// <summary>
    /// rAthena <c>unit_changetarget</c> foreachinrange — sweep every
    /// mob within <paramref name="range"/> of <paramref name="center"/>
    /// that is currently chasing <paramref name="oldTarget"/> and
    /// retarget it onto <paramref name="newTarget"/>. Returns the
    /// number of mobs whose target was switched. Used by KO_GENWAKU
    /// (Illusion Bewitch) and similar "redirect aggro" skills.
    /// </summary>
    int RetargetMobsChasing(Entity center, short range, Entity oldTarget, Entity newTarget);
}
