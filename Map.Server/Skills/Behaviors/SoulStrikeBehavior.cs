using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_SOULSTRIKE (id 13) — Mage Soul Strike. rAthena
/// <c>skill.cpp:case MG_SOULSTRIKE</c>: N-hit Ghost-element magic.
/// Hit count = <c>ceil(lv/2)</c> (lv1-2 = 1 bolt, lv3-4 = 2, ..., lv9-10 = 5).
/// </summary>
public sealed class SoulStrikeBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_SOULSTRIKE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = (skillLevel + 1) / 2;
        var basePerHit = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (basePerHit <= 0) basePerHit = 1;
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, basePerHit, source);
        }
        return true;
    }
}
