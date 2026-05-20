using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_BODYPAINT — auto-generated stub from
/// <c>src/map/skills/thief/bodypainting.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BodyPainting : SkillImpl
{
    public BodyPainting() : base(SkillIds.SC_BODYPAINT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if( flag&1 ) {
    // 		if (tsc && ((tsc->option&(OPTION_HIDE|OPTION_CLOAK)) || tsc->getSCE(SC_CAMOUFLAGE) || tsc->getSCE(SC_STEALTHFIELD))) {
    // 			status_change_end(target,SC_HIDING);
    // 			status_change_end(target,SC_CLOAKING);
    // 			status_change_end(target,SC_CLOAKINGEXCEED);
    // 			status_change_end(target,SC_CAMOUFLAGE);
    // 			status_change_end(target,SC_NEWMOON);
    // 			if (tsc && tsc->getSCE(SC__SHADOWFORM) && rnd() % 100 < 100 - tsc->getSCE(SC__SHADOWFORM)->val1 * 10) // [100 - (Skill Level x 10)] %
    // 				status_change_end(target, SC__SHADOWFORM);
    // 		}
    // 		// Attack Speed decrease and Blind happen to everyone around caster, not just hidden targets.
    // 		sc_start(src, target, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		sc_start(src, target, SC_BLIND, 53 + 2 * skill_lv, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	} else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), 0);
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR,
    // 			src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 	}
    }
}
