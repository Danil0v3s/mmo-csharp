using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SKE_STAR_CANNON — auto-generated stub from
/// <c>src/map/skills/taekwon/starcannon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StarCannon : SkillImpl
{
    public StarCannon() : base(SkillIds.SKE_STAR_CANNON) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // unit_data* ud = unit_bl2ud( src );
    // 
    // 	if( ud == nullptr ){
    // 		return;
    // 	}
    // 
    // 	for( const std::shared_ptr<s_skill_unit_group>& sug : ud->skillunits ){
    // 		if( sug->skill_id != SKE_TWINKLING_GALAXY ){
    // 			continue;
    // 		}
    // 
    // 		skill_unit* su = sug->unit;
    // 
    // 		if( distance_xy( x, y, su->x, su->y ) > skill_get_unit_range( sug->skill_id, sug->skill_lv ) ){
    // 			continue;
    // 		}
    // 
    // 		std::shared_ptr<s_skill_unit_group> sg = su->group;
    // 
    // 		for( int32 i = 0; i< MAX_SKILLTIMERSKILL; i++ ){
    // 			if( ud->skilltimerskill[i] == nullptr ){
    // 				continue;
    // 			}
    // 
    // 			if( ud->skilltimerskill[i]->skill_id != SKE_TWINKLING_GALAXY ){
    // 				continue;
    // 			}
    // 
    // 			delete_timer(ud->skilltimerskill[i]->timer, skill_timerskill);
    // 			ers_free(skill_timer_ers, ud->skilltimerskill[i]);
    // 			ud->skilltimerskill[i] = nullptr;
    // 		}
    // 
    // 		skill_delunitgroup(sg);
    // 
    // 		for (int32 i = 0; i < skill_get_time(getSkillId(), skill_lv) / skill_get_unit_interval(getSkillId()); i++)
    // 			skill_addtimerskill(src, tick + (t_tick)i*skill_get_unit_interval(getSkillId()), 0, x, y, getSkillId(), skill_lv, 0, flag);
    // 		flag |= 1;
    // 		skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 250 + 550 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SKE_SKY_MASTERY) * 5 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
