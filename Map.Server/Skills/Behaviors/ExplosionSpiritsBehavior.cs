using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_EXPLOSIONSPIRITS (id 270) — Monk Explosion Spirits. rAthena
/// <c>skill.cpp:case MO_EXPLOSIONSPIRITS</c>: applies
/// <see cref="StatusType.Explosionspirits"/> on the caster.
/// +<c>lv * 20</c> Crit, +<c>lv * 50</c> ATK while active.
/// Duration <c>180_000</c> ms.
///
/// Required prereq for Asura Strike (the SC tells MO_EXTREMITYFIST
/// the caster is in finisher stance).
/// </summary>
public sealed class ExplosionSpiritsBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_EXPLOSIONSPIRITS;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        // Val1 = crit boost; Val2 = ATK boost. SC handler reads both.
        ctx.Sc.Start(source, StatusType.Explosionspirits,
            val1: skillLevel * 20, val2: skillLevel * 50, val3: 0, val4: 0,
            durationMs: 180_000, source);
        return true;
    }
}
