using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_RADIANT_SPEAR — Imperial Guard Radiant Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/radiantspear.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 3500 + 1150*lv) + 5*POW</c>; plus
/// <c>+50 * IG_SPEAR_SWORD_M_lv</c> from the skill tree;
/// <c>+250*lv</c> when <see cref="StatusType.SpearScar"/> is active.</para>
/// </summary>
public sealed class RadiantSpear : RecursiveDamageSplashSkillImpl
{
    public RadiantSpear() : base(SkillIds.IG_RADIANT_SPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 3500 + 1150 * skillLevel) + 5 * src.Stats.Pow;
        if (src is PlayerEntity sd)
            ratio += 50 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SPEAR_SWORD_M) ?? 0);
        if (ctx.Sc?.Get(src, StatusType.SpearScar) != null)
            ratio += 250 * skillLevel;
        return ratio;
    }
}
