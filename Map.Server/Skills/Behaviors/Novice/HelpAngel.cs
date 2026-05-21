using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// NV_HELPANGEL — Novice Help Angel. Manual port of
/// <c>rathena-fork/src/map/skills/novice/helpangel.cpp</c>.
/// Defers to the StatusSkillImpl SC-apply path (or splashes party
/// members when the caster is in a party). Party splash is TODO.
/// </summary>
public sealed class HelpAngel : StatusSkillImpl
{
    public HelpAngel() : base(SkillIds.NV_HELPANGEL) { }
}
