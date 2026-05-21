using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_ALCHEMIST (mob.cpp:4375-4376) — fires when:
/// <list type="bullet">
///   <item>The mob is a summoned creature (<c>special_state.ai != AI_NONE</c>).</item>
///   <item>It is NOT currently in a trick-cast state (<c>trickcasting == 0</c>).</item>
///   <item>It has taken damage (<c>hp &lt; max_hp</c>).</item>
/// </list>
///
/// <para>Used by Alchemist summons (Cannibalize, Sphere, Bomb)
/// to self-heal or trigger their special on first damage. The three
/// gates together prevent the skill firing on a freshly spawned,
/// undamaged sphere or on a mob already mid-cast.</para>
/// </summary>
public sealed class AlchemistCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.Alchemist;

    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (mob.SpecialAi == MobSpecialAi.None) return false;
        if (mob.TrickCasting > 0) return false;
        return mob.Hp < mob.MaxHp;
    }
}
