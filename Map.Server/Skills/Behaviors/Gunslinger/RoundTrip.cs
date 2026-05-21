using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_R_TRIP — Rebellion Round Trip. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/roundtrip.cpp</c>.
/// Ratio <c>+(-100 + 350*lv)</c>.
/// </summary>
public sealed class RoundTrip : RecursiveDamageSplashSkillImpl
{
    public RoundTrip() : base(SkillIds.RL_R_TRIP) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
