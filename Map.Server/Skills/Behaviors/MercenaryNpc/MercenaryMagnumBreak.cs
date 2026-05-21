using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_MAGNUM — Mercenary Magnum Break. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_magnumbreak.cpp</c>.
/// Outer 5×5 splash: ratio <c>+10*lv</c>. Inner 3×3 takes +20*lv (TODO,
/// requires miscflag plumbing). Hit chance bonus <c>+10*lv%</c>.
/// Self-fire-element buff on cast (SC_FIREWEAPON).
/// </summary>
public sealed class MercenaryMagnumBreak : RecursiveDamageSplashSkillImpl
{
    public MercenaryMagnumBreak() : base(SkillIds.MS_MAGNUM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 10 * skillLevel / 100);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, src, skillLevel, ctx);
        ctx.Sc?.Start(src, StatusType.Fireweapon, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
