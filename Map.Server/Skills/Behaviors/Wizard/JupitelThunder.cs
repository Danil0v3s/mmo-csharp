using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Wizard;

/// <summary>
/// WZ_JUPITEL — Wizard Jupitel Thunder. Mirrors
/// <c>rathena-fork/src/map/skills/wizard/jupitelthunder.cpp</c>.
///
/// Multi-hit Wind magic + knockback per hit. Hits = <c>1 + lv</c>.
/// Per-hit damage = (100 + 50*lv)% MATK.
/// </summary>
public sealed class JupitelThunder : SkillImpl
{
    public JupitelThunder() : base(SkillIds.WZ_JUPITEL) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var perHit = Math.Max(1, matk * (100 + 50 * skillLevel) / 100);
        var hitCount = 1 + skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
