using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_LIGHTNINGBOLT (id 20) — Mage Lightning Bolt. N-hit Wind magic,
/// same shape as <see cref="FireBoltBehavior"/>. rAthena:
/// <c>skill.cpp:case MG_LIGHTNINGBOLT</c>.
/// </summary>
public sealed class LightningBoltBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_LIGHTNINGBOLT;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var basePerHit = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (basePerHit <= 0) basePerHit = 1;
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, basePerHit, source);
        }
        return true;
    }
}
