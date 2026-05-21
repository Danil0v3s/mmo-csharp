using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KUNAIWAIKYOKU — Kunai Distortion. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunaidistortion.cpp</c>.
/// POS2 splash; ratio <c>+(-100 + 300 + 600*lv) + 5*pow</c>.
/// SS_KUNAIKUSSETSU partner bonus + mirage cast + alt-flag scaling
/// are TODO.
/// </summary>
public sealed class KunaiDistortion : SkillImpl
{
    public KunaiDistortion() : base(SkillIds.SS_KUNAIWAIKYOKU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 600 * skillLevel) + 5 * src.Stats.Pow;
}
