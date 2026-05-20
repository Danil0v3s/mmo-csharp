using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_CRIMSONROCK — auto-generated stub from
/// <c>src/map/skills/mage/crimsonrock.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CrimsonRock : RecursiveDamageSplashSkillImpl
{
    public CrimsonRock() : base(SkillIds.WL_CRIMSONROCK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 700 + 600 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
