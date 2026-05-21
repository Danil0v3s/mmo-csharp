using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_OVERBRAND — Royal Guard Over Brand. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/overbrand.cpp</c>.
/// Ratio <c>+(-100 + 350*lv)</c>; <c>+(-100 + 500*lv)</c> with
/// SC_OVERBRANDREADY. +50 per Spear Quicken level. SC + skill-tree
/// plumbing into ratio are TODO.
/// </summary>
public sealed class OverBrand : RecursiveDamageSplashSkillImpl
{
    public OverBrand() : base(SkillIds.LG_OVERBRAND) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
