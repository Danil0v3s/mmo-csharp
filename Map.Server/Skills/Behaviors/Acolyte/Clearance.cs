using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CLEARANCE — auto-generated stub from
/// <c>src/map/skills/acolyte/clearance.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Clearance : SkillImpl
{
    public Clearance() : base(SkillIds.AB_CLEARANCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 	map_session_data* dstsd = BL_CAST( BL_PC, target );
    // 	int32 i = 0;
    // 
    // 	if( flag&1 || (i = skill_get_splash(getSkillId(), skill_lv)) < 1 ) { // As of the behavior in official server Clearance is just a super version of Dispell skill. [Jobbie]
    // 
    // 		if( target->type != BL_MOB && battle_check_target(src,target,BCT_PARTY) <= 0 ) // Only affect mob or party.
    // 			return;
    // 
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 
    // 		if(rnd()%100 >= 60 + 8 * skill_lv) {
    // 			if (sd)
    // 				clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 
    // 		if(status_isimmune(target))
    // 			return;
    // 
    // 		//Remove bonus_script by Clearance
    // 		if (dstsd)
    // 			pc_bonus_script_clear(dstsd,BSF_REM_ON_CLEARANCE);
    // 
    // 		if(tsc == nullptr || tsc->empty())
    // 			return;
    // 
    // 		//Statuses change that can't be removed by Cleareance
    // 		for (const auto &it : status_db) {
    // 			sc_type status = static_cast<sc_type>(it.first);
    // 
    // 			if (!tsc->getSCE(status))
    // 				continue;
    // 
    // 			if (it.second->flag[SCF_NOCLEARANCE])
    // 				continue;
    // 
    // 			switch (status) {
    // 				case SC_WHISTLE:		case SC_ASSNCROS:		case SC_POEMBRAGI:
    // 				case SC_APPLEIDUN:		case SC_HUMMING:		case SC_DONTFORGETME:
    // 				case SC_FORTUNE:		case SC_SERVICE4U:
    // 					if (!battle_config.dispel_song || tsc->getSCE(status)->val4 == 0)
    // 						continue; //If in song area don't end it, even if config enatargeted
    // 					break;
    // 				case SC_ASSUMPTIO:
    // 					if (target->type == BL_MOB)
    // 						continue;
    // 					break;
    // 			}
    // 			if (status == SC_BERSERK || status == SC_SATURDAYNIGHTFEVER)
    // 				tsc->getSCE(status)->val2 = 0; //Mark a dispelled berserk to avoid setting hp to 100 by setting hp penalty to 0.
    // 			status_change_end(target,status);
    // 		}
    // 		return;
    // 	}
    // 
    // 	map_foreachinallrange(skill_area_sub, target, i, BL_CHAR, src, getSkillId(), skill_lv, tick, flag|1, skill_castend_damage_id);
    }
}
