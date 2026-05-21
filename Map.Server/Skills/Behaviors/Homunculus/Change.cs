using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HLIF_CHANGE — Lif Change. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_change.cpp</c>.
/// Renewal-only — defers to the StatusSkillImpl SC-apply path.
/// Pre-renewal full HP/SP heal is omitted (renewal default).
/// </summary>
public sealed class Change : StatusSkillImpl
{
    public Change() : base(SkillIds.HLIF_CHANGE) { }
}
