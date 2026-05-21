using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_WIZARD — Spirit Of The Wizard. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/spiritofthewizard.cpp</c>.
/// Status-only buff; grants SC_SMA on caster (handled at status layer).
/// </summary>
public sealed class SpiritoftheWizard : StatusSkillImpl
{
    public SpiritoftheWizard() : base(SkillIds.SL_WIZARD) { }
}
