using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_TRACKING — auto-generated stub from
/// <c>src/map/skills/gunslinger/tracking.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Tracking : WeaponSkillImpl
{
    public Tracking() : base(SkillIds.GS_TRACKING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 100 * (skill_lv + 1);
    return baseRatio;
    }
}
