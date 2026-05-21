using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_SUPERNOVICE — Spirit Of The Supernovice. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/spiritofthesupernovice.cpp</c>.
/// Status-only buff; 1% chance to erase die counter on success (TODO).
/// </summary>
public sealed class SpiritoftheSupernovice : StatusSkillImpl
{
    public SpiritoftheSupernovice() : base(SkillIds.SL_SUPERNOVICE) { }
}
