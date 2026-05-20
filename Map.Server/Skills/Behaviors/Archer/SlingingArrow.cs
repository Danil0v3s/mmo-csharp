using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_THROWARROW — auto-generated stub from
/// <c>src/map/skills/archer/slingingarrow.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SlingingArrow : WeaponSkillImpl
{
    public SlingingArrow() : base(SkillIds.DC_THROWARROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += 10 + 40 * skill_lv;
    // #else
    // 	base_skillratio += -40 + 40 * skill_lv;
    // #endif
    return baseRatio;
    }
}
