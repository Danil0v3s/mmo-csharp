using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_SHOWER — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_arrowshower.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryArrowShower : RecursiveDamageSplashSkillImpl
{
    public MercenaryArrowShower() : base(SkillIds.MA_SHOWER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += 50 + 10 * skill_lv;
    // #else
    // 	base_skillratio += -25 + 5 * skill_lv;
    // #endif
    return baseRatio;
    }
}
