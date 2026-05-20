using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_ASSIMILATEPOWER — auto-generated stub from
/// <c>src/map/skills/acolyte/assimilatepower.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AssimilatePower : SkillImpl
{
    public AssimilatePower() : base(SkillIds.SR_ASSIMILATEPOWER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (flag & 1) {
    // 		int32 amount = 0;
    // 
    // 		if (dstsd && (sd == dstsd || map_flag_vs(src->m)) && (dstsd->class_ & MAPID_FIRSTMASK) != MAPID_GUNSLINGER) {
    // 			if (dstsd->spiritball > 0) {
    // 				amount = dstsd->spiritball;
    // 				pc_delspiritball(dstsd, dstsd->spiritball, 0);
    // 			}
    // 			if (dstsd->spiritcharm_type != CHARM_TYPE_NONE && dstsd->spiritcharm > 0) {
    // 				amount += dstsd->spiritcharm;
    // 				pc_delspiritcharm(dstsd, dstsd->spiritcharm, dstsd->spiritcharm_type);
    // 			}
    // 		}
    // 
    // 		if (amount)
    // 			status_percent_heal(src, 0, amount);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, amount != 0);
    // 	} else {
    // 		clif_skill_damage(*src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE);
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), splash_target(src), src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | BCT_SELF | SD_SPLASH | 1, skill_castend_nodamage_id);
    // 	}
    }
}
