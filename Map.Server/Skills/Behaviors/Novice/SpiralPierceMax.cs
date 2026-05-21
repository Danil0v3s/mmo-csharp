using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_SPIRAL_PIERCE_MAX — Hyper Novice Spiral Pierce Max. Manual port
/// of <c>rathena-fork/src/map/skills/novice/spiralpiercemax.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 1500*lv) + 5*POW</c>; size multiplier
/// (Small 1.5×, Medium 1.3×, Large 1.2×) is TODO once Size is plumbed.
/// 100% SC_ANKLE on non-status-immune targets.
/// </summary>
public sealed class SpiralPierceMax : WeaponSkillImpl
{
    public SpiralPierceMax() : base(SkillIds.HN_SPIRAL_PIERCE_MAX) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 1500 * skillLevel) + 5 * src.Stats.Pow;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity && (target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        ctx.Sc?.Start(target, StatusType.Ankle, val1: 0, 0, 0, 0, durationMs: 10_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
