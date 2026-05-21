using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KIRIKAGE — Shadow Slash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowslash.cpp</c>.
/// Renewal: ratio <c>+(-50 + 150*lv)</c>. Ends SC_HIDING on cast.
/// Caster slide is TODO.
/// </summary>
public sealed class ShadowSlash : WeaponSkillImpl
{
    public ShadowSlash() : base(SkillIds.NJ_KIRIKAGE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-50 + 150 * skillLevel);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.Hiding);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
