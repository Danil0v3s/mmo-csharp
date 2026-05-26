using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_SHIELD_SHOOTING — Imperial Guard Shield Shooting. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldshooting.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1000 + 3500*lv) + 10*POW</c>; plus
/// <c>+150*lv * IG_SHIELD_MASTERY_lv</c>.</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena also adds shield weight *7/60 and
/// <c>refine*100</c>; requires <c>IEquipService.FindEquipped(HandL)</c>
/// threading.</para>
/// </summary>
public sealed class ShieldShooting : RecursiveDamageSplashSkillImpl
{
    public ShieldShooting() : base(SkillIds.IG_SHIELD_SHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 1000 + 3500 * skillLevel) + 10 * src.Stats.Pow;
        if (src is PlayerEntity sd)
            ratio += skillLevel * 150 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SHIELD_MASTERY) ?? 0);
        return ratio;
    }
}
