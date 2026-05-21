using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_SPREADATTACK — Gunslinger Spread Attack. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/spreadattack.cpp</c>.
/// Renewal ratio <c>+30*lv</c>.
/// </summary>
public sealed class SpreadAttack : RecursiveDamageSplashSkillImpl
{
    public SpreadAttack() : base(SkillIds.GS_SPREADATTACK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;
}
