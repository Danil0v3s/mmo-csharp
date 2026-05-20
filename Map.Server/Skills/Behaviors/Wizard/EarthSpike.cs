using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Wizard;

/// <summary>
/// WZ_EARTHSPIKE — Wizard Earth Spike. Mirrors
/// <c>rathena-fork/src/map/skills/wizard/earthspike.cpp</c>.
///
/// N-hit single-target Earth magic. Hit count = skill level. Per-hit
/// = (100 + 100*lv)% MATK midpoint.
/// </summary>
public sealed class EarthSpike : SkillImpl
{
    public EarthSpike() : base(SkillIds.WZ_EARTHSPIKE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var perHit = Math.Max(1, matk * (100 + 100 * skillLevel) / 100);
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
