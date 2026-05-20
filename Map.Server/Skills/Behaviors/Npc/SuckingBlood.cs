using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_BLOODDRAIN — auto-generated stub from
/// <c>src/map/skills/npc/suckingblood.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SuckingBlood : SkillImpl
{
    public SuckingBlood() : base(SkillIds.NPC_BLOODDRAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 100 * (skill_lv - 1);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 heal = (int32)skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	if (heal > 0){
    // 		clif_skill_nodamage(nullptr, *src, AL_HEAL, heal);
    // 		status_heal(src, heal, 0, 0);
    // 	}
    }
}
