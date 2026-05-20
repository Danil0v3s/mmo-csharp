using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AS_SONICBLOW (id 136) — Assassin Sonic Blow. rAthena
/// <c>skill.cpp:case AS_SONICBLOW</c>: 8-hit chain on a single target.
/// Per-hit damage = (300 + 40 * lv)% / 8 = ~33.75% + 5%/lv per hit
/// (so total ramps from 300% at lv1 to 700% at lv10).
///
/// Requires katar weapon (gated upstream at requirement-check; the
/// plugin assumes valid weapon).
/// </summary>
public sealed class SonicBlowBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AS_SONICBLOW;

    /// <summary>Hit count is fixed at 8 across all levels in rAthena.</summary>
    private const int HitCount = 8;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Total rate (300 + 40 * lv) split across 8 hits.
        var totalRate = 300 + 40 * skillLevel;
        var perHitRate = totalRate / HitCount;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(source, target);
            var dmg = (int)Math.Clamp(swing.Total * perHitRate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, source);
        }
        return true;
    }
}
