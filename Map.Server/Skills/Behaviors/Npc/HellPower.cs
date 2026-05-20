using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_HELLPOWER — auto-generated stub from
/// <c>src/map/skills/npc/hellpower.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HellPower : WeaponSkillImpl
{
    public HellPower() : base(SkillIds.NPC_HELLPOWER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv,
    // 		sc_start(src, target, skill_get_sc(getSkillId()), skill_lv*20, skill_lv, skill_get_time2(getSkillId(), skill_lv)));
    }
}
