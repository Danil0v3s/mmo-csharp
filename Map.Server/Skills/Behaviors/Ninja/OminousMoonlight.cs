using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_IZAYOI — Ominous Moonlight (16th Night). Manual port of
/// <c>rathena-fork/src/map/skills/ninja/16thnight.cpp</c>.
/// Status-only buff.
/// </summary>
public sealed class OminousMoonlight : StatusSkillImpl
{
    public OminousMoonlight() : base(SkillIds.KO_IZAYOI) { }
}
