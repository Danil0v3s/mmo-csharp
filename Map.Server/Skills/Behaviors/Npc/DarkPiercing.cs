using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_DARKPIERCING — auto-generated stub from
/// <c>src/map/skills/npc/darkpiercing.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DarkPiercing : SkillImpl
{
    public DarkPiercing() : base(SkillIds.NPC_DARKPIERCING) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = target->id;
    // 	if (battle_config.skill_eightpath_algorithm) {
    // 		//Use official AoE algorithm
    // 		if (!(map_foreachindir(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 		   skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), 0, splash_target(src),
    // 		   skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY))) {
    // 
    // 			//These skills hit at least the target if the AoE doesn't hit
    // 			skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 		}
    // 	} else {
    // 		map_foreachinpath(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), splash_target(src),
    // 			skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    // 	}
    }
}
