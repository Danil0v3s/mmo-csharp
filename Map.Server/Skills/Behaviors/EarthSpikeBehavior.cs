using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// WZ_EARTHSPIKE (id 90) — Wizard Earth Spike. rAthena
/// <c>skill.cpp:case WZ_EARTHSPIKE</c>: N-hit single-target Earth
/// magic. Hit count = skill level (lv1 = 1, lv5 = 5). Per-hit
/// damage = (100 + 100 * lv)% MATK Earth.
/// </summary>
public sealed class EarthSpikeBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.WZ_EARTHSPIKE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var perHit = Math.Max(1, matk * (100 + 100 * skillLevel) / 100);
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, source);
        }
        return true;
    }
}
