using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KG_KAGEMUSYA — Shadow Warrior. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowwarrior.cpp</c>.
/// Status-only buff (SC_KAGEMUSYA).
/// </summary>
public sealed class ShadowWarrior : StatusSkillImpl
{
    public ShadowWarrior() : base(SkillIds.KG_KAGEMUSYA) { }
}
