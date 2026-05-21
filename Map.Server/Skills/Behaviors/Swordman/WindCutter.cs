using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_WINDCUTTER — Rune Knight Wind Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/windcutter.cpp</c>.
/// Ratio depends on equipped weapon type:
///   W_2HSWORD → +(-100 + 250*lv)
///   W_1HSPEAR / W_2HSPEAR → +(-100 + 400*lv)
///   else → +(-100 + 300*lv)
/// Weapon-type query is TODO; we use the default 300*lv ratio.
/// </summary>
public sealed class WindCutter : RecursiveDamageSplashSkillImpl
{
    public WindCutter() : base(SkillIds.RK_WINDCUTTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
