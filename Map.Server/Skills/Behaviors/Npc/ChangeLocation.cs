using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_MOVE_COORDINATE — auto-generated stub from
/// <c>src/map/skills/npc/changelocation.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ChangeLocation : SkillImpl
{
    public ChangeLocation() : base(SkillIds.NPC_MOVE_COORDINATE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int16 px = target->x, py = target->y;
    // 	if (!skill_check_unit_movepos(0, target, src->x, src->y, 1, 1)) {
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	clif_blown(target);
    // 
    // 	// If caster is not a boss, switch coordinates with the target
    // 	if (status_get_class_(src) != CLASS_BOSS) {
    // 		if (!skill_check_unit_movepos(0, src, px, py, 1, 1)) {
    // 			flag |= SKILL_NOCONSUME_REQ;
    // 			return;
    // 		}
    // 
    // 		clif_blown(src);
    // 	}
    }
}
