using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// WZ_JUPITEL (id 84) — Wizard Jupitel Thunder. rAthena
/// <c>skill.cpp:case WZ_JUPITEL</c>: multi-hit Wind magic that
/// knocks the target back per hit. Hit count = <c>1 + skill_lv</c>
/// (lv1 = 2 hits, lv10 = 11 hits). Per-hit damage scales with MATK.
///
/// Knockback ports separately (movement-direction helper); plugin
/// records the damage hits.
/// </summary>
public sealed class JupitelThunderBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.WZ_JUPITEL;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        // rAthena: per-hit = (100 + 50*lv)% MATK.
        var perHit = Math.Max(1, matk * (100 + 50 * skillLevel) / 100);
        var hitCount = 1 + skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, source);
        }
        return true;
    }
}
