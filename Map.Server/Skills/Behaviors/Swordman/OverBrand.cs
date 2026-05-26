using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_OVERBRAND — Royal Guard Over Brand. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/overbrand.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 350*lv)</c> baseline; <c>+(-100 + 500*lv)</c>
/// when <see cref="StatusType.Overbrandready"/> is active. Plus
/// <c>+50 * CR_SPEARQUICKEN_lv</c> from the caster's skill tree.</para>
/// </summary>
public sealed class OverBrand : RecursiveDamageSplashSkillImpl
{
    public OverBrand() : base(SkillIds.LG_OVERBRAND) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ready = ctx.Sc?.Get(src, StatusType.Overbrandready) != null;
        var ratio = baseRatio + (ready ? (-100 + 500 * skillLevel) : (-100 + 350 * skillLevel));
        if (src is PlayerEntity sd)
            ratio += 50 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.CR_SPEARQUICKEN) ?? 0);
        return ratio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
