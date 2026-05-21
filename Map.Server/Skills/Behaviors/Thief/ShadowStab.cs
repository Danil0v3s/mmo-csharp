using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_SHADOW_STAB — Shadow Stab. Manual port of
/// <c>rathena-fork/src/map/skills/thief/shadowstab.cpp</c>.
/// Ratio <c>+(-100 + 550*lv) + 5*pow</c>; +100*lv +2*pow under
/// SC_CLOAKINGEXCEED. Ends Cloaking on cast.
/// </summary>
public sealed class ShadowStab : WeaponSkillImpl
{
    public ShadowStab() : base(SkillIds.SHC_SHADOW_STAB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 550 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.Cloaking);
        ctx.Sc?.End(src, StatusType.Cloakingexceed);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
