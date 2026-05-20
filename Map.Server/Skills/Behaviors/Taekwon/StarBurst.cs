using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SKE_STAR_BURST — auto-generated stub from
/// <c>src/map/skills/taekwon/starburst.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StarBurst : SkillImpl
{
    public StarBurst() : base(SkillIds.SKE_STAR_BURST) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (flag & 1) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	} else {
    // 		unit_data* ud = unit_bl2ud( src );
    // 
    // 		if( ud != nullptr ){
    // 			for( const std::shared_ptr<s_skill_unit_group>& sug : ud->skillunits ){
    // 				if( sug->skill_id != SKE_TWINKLING_GALAXY ){
    // 					continue;
    // 				}
    // 
    // 				skill_unit* su = sug->unit;
    // 
    // 				// Check if it is too far away
    // 				if( distance_xy( target->x, target->y, su->x, su->y ) > skill_get_unit_range( sug->skill_id, sug->skill_lv ) ){
    // 					continue;
    // 				}
    // 
    // 				std::shared_ptr<s_skill_unit_group> sg = su->group;
    // 
    // 				for( int32 i = 0; i < MAX_SKILLTIMERSKILL; i++ ){
    // 					if( ud->skilltimerskill[i] == nullptr ){
    // 						continue;
    // 					}
    // 
    // 					if( ud->skilltimerskill[i]->skill_id != sug->skill_id ){
    // 						continue;
    // 					}
    // 
    // 					delete_timer(ud->skilltimerskill[i]->timer, skill_timerskill);
    // 					ers_free(skill_timer_ers, ud->skilltimerskill[i]);
    // 					ud->skilltimerskill[i] = nullptr;
    // 				}
    // 
    // 				skill_delunitgroup(sg);
    // 				sc_start2(src, target, skill_get_sc(getSkillId()), 100, skill_lv, src->id, skill_get_time2(getSkillId(), skill_lv));
    // 
    // 				skill_castend_pos2(src, target->x, target->y, getSkillId(), skill_lv, tick, 0);
    // 				return;
    // 			}
    // 		}
    // 
    // 		if( sd != nullptr ){
    // 			clif_skill_fail(*sd, getSkillId(), USESKILL_FAIL_LEVEL);
    // 		}
    // 
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 	}
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag |= 1;
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 500 + 400 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SKE_SKY_MASTERY) * 3 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
