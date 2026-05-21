using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MYSTATUSON (mob.cpp:4340) — fires when the mob has
/// SC <c>cond2</c> active. Special case: <c>cond2 == 0</c> (SC_NONE)
/// fires when the mob has ANY common SC, mirroring the
/// SC_COMMON_MIN..SC_COMMON_MAX loop in rAthena
/// <c>mob_getstatus_sub</c> (mob.cpp:4142).
/// </summary>
public sealed class MyStatusOnCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MyStatusOn;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Sc == null) return false;
        if (entry.Cond2 == 0) return HasAnyCommonStatus(mob, context.Sc);
        return context.Sc.Get(mob, (StatusType)entry.Cond2) != null;
    }

    /// <summary>rAthena SC_COMMON_MIN..SC_COMMON_MAX ≈ Stone..Confusion.</summary>
    private static bool HasAnyCommonStatus(MobEntity mob, IStatusChangeService sc)
    {
        // SC_COMMON_MIN/MAX in rAthena's enum is the contiguous
        // block of debuff SCs (Stone, Freeze, Stun, Sleep, Poison,
        // Curse, Silence, Confusion, Bleeding). Iterate the block.
        for (int i = (int)StatusType.Stone; i <= (int)StatusType.Bleeding; i++)
        {
            if (sc.Get(mob, (StatusType)i) != null) return true;
        }
        return false;
    }
}
