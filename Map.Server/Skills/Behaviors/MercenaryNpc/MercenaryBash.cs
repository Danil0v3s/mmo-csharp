using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_BASH — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_bash.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryBash : WeaponSkillImpl
{
    public MercenaryBash() : base(SkillIds.MS_BASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // It is proven that bonus is applied on final hitrate, not hit.
    // 	// Base 100% + 30% per level
    // 	base_skillratio += 30 * skill_lv;
    return baseRatio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // +5% hit per level
    // 	hit_rate += hit_rate * 5 * skill_lv / 100;
    return hitRate;
    }
}
