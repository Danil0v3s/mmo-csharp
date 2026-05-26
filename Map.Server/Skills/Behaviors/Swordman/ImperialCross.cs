using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_IMPERIAL_CROSS — Imperial Guard Imperial Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/imperialcross.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1650 + 1350*lv) + 5*POW</c>; plus
/// <c>+25 * IG_SPEAR_SWORD_M_lv</c> from the skill tree;
/// <c>+(100 + 300*lv)</c> when <see cref="StatusType.SpearScar"/> is
/// active.</para>
/// </summary>
public sealed class ImperialCross : WeaponSkillImpl
{
    public ImperialCross() : base(SkillIds.IG_IMPERIAL_CROSS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 1650 + 1350 * skillLevel) + 5 * src.Stats.Pow;
        if (src is PlayerEntity sd)
            ratio += 25 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SPEAR_SWORD_M) ?? 0);
        if (ctx.Sc?.Get(src, StatusType.SpearScar) != null)
            ratio += 100 + 300 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
