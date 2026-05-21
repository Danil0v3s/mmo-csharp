using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_RUN — Sprint. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/run.cpp</c>.
/// Toggles SC_RUN on the target. Walkok re-send is TODO.
/// </summary>
public sealed class Run : StatusSkillImpl
{
    public Run() : base(SkillIds.TK_RUN) { }
}
