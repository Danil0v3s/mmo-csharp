using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_IGNITIONBREAK — Rune Knight Ignition Break. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/ignitionbreak.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 450*lv)</c>. Splash dispatch rides on
/// <see cref="RecursiveDamageSplashSkillImpl"/>, which walks victims
/// via <see cref="IEntityRegistry.ForEachInRange"/> — matches the
/// rAthena <c>map_foreachinrange(skill_area_sub, …, BCT_ENEMY | 1)</c>
/// shape (BCT_ENEMY filter, friendly-fire excluded).</para>
/// </summary>
public sealed class IgnitionBreak : RecursiveDamageSplashSkillImpl
{
    public IgnitionBreak() : base(SkillIds.RK_IGNITIONBREAK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
