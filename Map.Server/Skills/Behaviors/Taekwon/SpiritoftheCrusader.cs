using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_CRUSADER — Spirit Of The Crusader. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/spiritofthecrusader.cpp</c>.
/// Status-only buff.
/// </summary>
public sealed class SpiritoftheCrusader : StatusSkillImpl
{
    public SpiritoftheCrusader() : base(SkillIds.SL_CRUSADER) { }
}
