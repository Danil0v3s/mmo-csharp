using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_DUAL_CANNON_FIRE — auto-generated stub from
/// <c>src/map/skills/other/dualcannonfire.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DualCannonFire : WeaponSkillImpl
{
    public DualCannonFire() : base(SkillIds.ABR_DUAL_CANNON_FIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // TODO: Need official formula.
    // 	base_skillratio += -100 + 8000;
    return baseRatio;
    }
}
