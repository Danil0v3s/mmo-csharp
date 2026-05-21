using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_MONK — Spirit Of The Monk. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/spiritofthemonk.cpp</c>.
/// Status-only buff; grants SC_SMA on the caster as a follow-up.
/// </summary>
public sealed class SpiritoftheMonk : StatusSkillImpl
{
    public SpiritoftheMonk() : base(SkillIds.SL_MONK) { }
}
