using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob.Slaves;

/// <summary>
/// Read-mostly helpers for slave-mob / friend / master lookups.
/// Mirrors rAthena <c>mob_countslave</c> (<c>mob.cpp:3946</c>),
/// <c>mob_getfriendhprate</c> (<c>mob.cpp:4114</c>),
/// <c>mob_getfriendstatus</c> (<c>mob.cpp:4196</c>),
/// <c>mob_getmasterhpltmaxrate</c> (<c>mob.cpp:4130</c>).
///
/// <para>Friend/master identification rides on
/// <see cref="Entity.MasterId"/> (the canonical slave-owner field
/// shared with <c>SummonAiService</c>). The "friend" universe for a
/// mob is the set of other live mobs sharing its master plus the
/// master itself when the master is a player and the mob is a
/// summoned creature (<c>special_state.ai != AI_NONE</c>) — rAthena
/// <c>mob.cpp:4121</c> flips the search bucket to BL_PC in that case.</para>
/// </summary>
public interface ISlaveMobService
{
    /// <summary>
    /// Count of live slaves owned by <paramref name="master"/> on the
    /// same map. rAthena <c>mob_countslave(bl)</c>.
    /// </summary>
    int CountSlaves(Entity master);

    /// <summary>
    /// First friend mob within 8 tiles whose HP% falls in the
    /// <paramref name="minRate"/>..<paramref name="maxRate"/> window
    /// (both inclusive). Mirrors <c>mob_getfriendhprate</c>: the search
    /// radius is fixed at 8 cells; friend-set is BL_MOB by default, or
    /// BL_PC when the mob is a player-summoned creature.
    /// </summary>
    Entity? GetFriendByHpRate(MobEntity mob, int minRate, int maxRate);

    /// <summary>
    /// First friend mob within 8 tiles whose SC presence matches
    /// (cond=ON means "has it", cond=OFF means "doesn't have it").
    /// Mirrors <c>mob_getfriendstatus</c>.
    /// </summary>
    MobEntity? GetFriendByStatus(MobEntity mob, MobSkillCondition cond, StatusType type);

    /// <summary>
    /// The master entity if its HP% is below <paramref name="rate"/>,
    /// otherwise null. Mirrors <c>mob_getmasterhpltmaxrate</c>.
    /// </summary>
    Entity? GetMasterIfHpBelow(MobEntity mob, int rate);
}
