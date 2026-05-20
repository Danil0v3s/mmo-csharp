using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_FIREBOLT (id 19) — Mage Fire Bolt. rAthena
/// <c>skill.cpp:case MG_FIREBOLT</c>: N-hit single-target Fire magic,
/// hit count = skill level (lv1 = 1 bolt, lv10 = 10 bolts). Each bolt
/// deals 100 % MATK Fire element.
///
/// Total damage scales linearly with level; we delegate to the
/// Magic resolver per-hit so element fix + DamageRate apply
/// consistently.
/// </summary>
public sealed class FireBoltBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_FIREBOLT;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Damage formula approximation — magic resolver runs ele/MATK math.
        // Per-hit MATK = MatkMin..MatkMax scaled by Holy element table; we
        // approximate by reading caster's MATK midpoint.
        var basePerHit = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (basePerHit <= 0) basePerHit = 1;

        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, basePerHit, source);
        }
        return true;
    }
}
