using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_SPREADATTACK — auto-generated stub from
/// <c>src/map/skills/gunslinger/spreadattack.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpreadAttack : RecursiveDamageSplashSkillImpl
{
    public SpreadAttack() : base(SkillIds.GS_SPREADATTACK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += 30 * skill_lv;
    // #else
    // 	base_skillratio += 20 * (skill_lv - 1);
    // #endif
    return baseRatio;
    }
}
