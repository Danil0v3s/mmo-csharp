using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_HUNDREDSPEAR — Rune Knight Hundred Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/hundredspear.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 600 + 200*lv) + 50*SpiralPierce_lv</c>;
/// with <see cref="StatusType.DragonicAura"/>, <c>+160*aura_lv</c>;
/// with <see cref="StatusType.ChargingpierceCount"/> <c>val1 ≥ 10</c>
/// the running ratio doubles.</para>
/// </summary>
public sealed class HundredSpear : RecursiveDamageSplashSkillImpl
{
    public HundredSpear() : base(SkillIds.RK_HUNDREDSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 600 + 200 * skillLevel);
        if (src is PlayerEntity sd)
            ratio += 50 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.LK_SPIRALPIERCE) ?? 0);
        var aura = ctx.Sc?.Get(src, StatusType.DragonicAura);
        if (aura != null) ratio += aura.Val1 * 160;
        var charging = ctx.Sc?.Get(src, StatusType.ChargingpierceCount);
        if (charging != null && charging.Val1 >= 10) ratio *= 2;
        return ratio;
    }
}
