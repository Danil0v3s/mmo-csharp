using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_INFRAREDSCAN — auto-generated stub from
/// <c>src/map/skills/merchant/infraredscan.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class InfraredScan : SkillImpl
{
    public InfraredScan() : base(SkillIds.NC_INFRAREDSCAN) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* tsc = status_get_sc(target);
    // 
    // 	if (flag & 1) {
    // 		status_change_end(target, SC_HIDING);
    // 		status_change_end(target, SC_CLOAKING);
    // 		status_change_end(target, SC_CLOAKINGEXCEED);
    // 		status_change_end(target, SC_CAMOUFLAGE);
    // 		status_change_end(target, SC_NEWMOON);
    // 		if (tsc && tsc->getSCE(SC__SHADOWFORM) && rnd() % 100 < 100 - tsc->getSCE(SC__SHADOWFORM)->val1 * 10) {// [100 - (Skill Level x 10)] %
    // 			status_change_end(target, SC__SHADOWFORM);
    // 		}
    // 		sc_start(src, target, SC_INFRAREDSCAN, 10000, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	} else {
    // 		clif_skill_damage(*src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE);
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), splash_target(src), src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
    }
}
