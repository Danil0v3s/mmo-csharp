using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_HAMMER_OF_GOD — Rebellion Hammer of God. Port of
/// <c>rathena-fork/src/map/skills/gunslinger/hammerofgod.cpp</c>.
/// Ratio: <c>-100 + 100·lv + 150·coins</c> (or <c>+400·coins</c> if
/// target carries SC_C_MARKER from the caster). Coin count reads
/// <c>PlayerEntity.SpiritBall</c> (rAthena <c>spiritball_old</c>);
/// non-PC callers fall back to 5.
/// </summary>
public sealed class HammerOfGod : RecursiveDamageSplashSkillImpl
{
    public HammerOfGod() : base(SkillIds.RL_HAMMER_OF_GOD) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int coins = src is PlayerEntity pc && pc.SpiritBall > 0 ? pc.SpiritBall : 5;
        int ratio = baseRatio + (-100 + 100 * skillLevel);
        // SC_C_MARKER from caster → bigger per-coin multiplier (400 vs 150).
        var cmarker = ctx.Sc?.Get(target, StatusType.CMarker);
        bool fromCaster = cmarker != null && cmarker.Val2 == src.Id.Value;
        ratio += (fromCaster ? 400 : 150) * coins;
        return ratio;
    }
}
