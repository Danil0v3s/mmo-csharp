using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SVG_SPIRIT — Summoner Spirit of Savage. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/spiritofsavage.cpp</c>.
/// Ratio <c>+(150 + 150*lv)</c>. Path-line splash + SU_SPIRITOFLIFE
/// HP-ratio multiplier are TODO.
/// </summary>
public sealed class SpiritofSavage : SkillImpl
{
    public SpiritofSavage() : base(SkillIds.SU_SVG_SPIRIT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 150 + 150 * skillLevel;
}
