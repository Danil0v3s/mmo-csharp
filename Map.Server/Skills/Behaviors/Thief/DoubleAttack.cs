using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_DOUBLE — Double Attack. Manual port of
/// <c>rathena-fork/src/map/skills/thief/doubleattack.cpp</c>.
/// Two-hit weapon proc from the passive autocast layer.
/// </summary>
public sealed class DoubleAttack : WeaponSkillImpl
{
    public DoubleAttack() : base(SkillIds.TF_DOUBLE) { }
}
