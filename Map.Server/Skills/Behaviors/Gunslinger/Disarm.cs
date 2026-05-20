using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DISARM — auto-generated stub from
/// <c>src/map/skills/gunslinger/disarm.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Disarm : WeaponSkillImpl
{
    public Disarm() : base(SkillIds.GS_DISARM) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_strip_equip(src, target, getSkillId(), skill_lv);
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    }
}
