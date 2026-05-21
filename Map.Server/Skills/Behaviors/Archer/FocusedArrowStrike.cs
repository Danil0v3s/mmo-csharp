using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_SHARPSHOOTING — Sniper Focused Arrow Strike. Manual port of
/// <c>rathena-fork/src/map/skills/archer/focusedarrowstrike.cpp</c>.
/// Renewal ratio: <c>+(-100 + 300 + 300*lv)</c>. Mob-cast variants
/// are TODO. Ends SC_CAMOUFLAGE on hit.
/// </summary>
public sealed class FocusedArrowStrike : RecursiveDamageSplashSkillImpl
{
    public FocusedArrowStrike() : base(SkillIds.SN_SHARPSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 300 + 300 * skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.Camouflage);
    }
}
