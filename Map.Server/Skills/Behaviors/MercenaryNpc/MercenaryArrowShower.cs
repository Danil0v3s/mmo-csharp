using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_SHOWER — Mercenary Arrow Shower. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_arrowshower.cpp</c>.
/// Renewal: ratio <c>+50 + 10*lv</c>.
/// </summary>
public sealed class MercenaryArrowShower : RecursiveDamageSplashSkillImpl
{
    public MercenaryArrowShower() : base(SkillIds.MA_SHOWER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 10 * skillLevel;
}
