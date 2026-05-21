using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// PA_PRESSURE — Paladin Gloria Domini / Pressure. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/gloriadomini.cpp</c>.
/// Renewal: ratio <c>+(-100 + 500 + 150*lv)</c>, dealt as BF_MAGIC.
/// </summary>
public sealed class GloriaDomini : SkillImpl
{
    public GloriaDomini() : base(SkillIds.PA_PRESSURE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 + 150 * skillLevel);
}
