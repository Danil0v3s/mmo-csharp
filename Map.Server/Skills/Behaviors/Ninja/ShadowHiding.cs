using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_YAMIKUMO — Shadow Hiding. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowhiding.cpp</c>.
/// Toggles SC_YAMIKUMO on the target. Animation only here; the
/// dedicated SC enum is not yet exposed.
/// </summary>
public sealed class ShadowHiding : StatusSkillImpl
{
    public ShadowHiding() : base(SkillIds.KO_YAMIKUMO) { }
}
