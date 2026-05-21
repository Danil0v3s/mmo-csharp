using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MOBNEARBYGT (mob.cpp:4377-4378) —
/// <c>map_foreachinallrange(mob_count_sub, md, AREA_SIZE, BL_MOB) &gt; cond2</c>.
///
/// <para>Note: rAthena's <c>mob_count_sub</c> reads up to 10 class-id
/// args from a va_list, but the picker call site passes NO extra args,
/// so the practical semantics are "any mob in AREA_SIZE" (≈14 cells),
/// not a class-filtered scan. We mirror that — count every live mob in
/// range, exclude self, compare to cond2.</para>
///
/// <para>If <see cref="MobConditionContext.Entities"/> is null we
/// return false rather than throw (defensive for tests that don't
/// wire the spatial index, mirroring the other registry-backed
/// evaluators like Friend/Master).</para>
/// </summary>
public sealed class MobNearbyGreaterCondition : IMobSkillConditionEvaluator
{
    /// <summary>rAthena <c>AREA_SIZE</c> from map.hpp.</summary>
    public const short AreaSize = 14;

    public MobSkillCondition Kind => MobSkillCondition.MobNearbyGreater;

    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Entities == null) return false;

        var nearby = context.Entities.ForEachInRange(
            mob.MapId, mob.X, mob.Y, AreaSize, EntityType.Mob);

        int count = 0;
        foreach (var e in nearby)
        {
            if (e is not MobEntity m) continue;
            if (m.Id == mob.Id) continue;   // exclude self
            if (m.Hp <= 0) continue;        // dead mobs don't count
            count++;
        }

        return count > entry.Cond2;
    }
}
