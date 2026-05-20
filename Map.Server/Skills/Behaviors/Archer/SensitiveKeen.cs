using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_SENSITIVEKEEN — auto-generated stub from
/// <c>src/map/skills/archer/sensitivekeen.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SensitiveKeen : WeaponSkillImpl
{
    public SensitiveKeen() : base(SkillIds.RA_SENSITIVEKEEN) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( rnd()%100 < 8 * skill_lv ) {
    // 		map_session_data* sd = BL_CAST( BL_PC, src );
    // 	
    // 		skill_castend_damage_id(src, target, RA_WUGBITE, ((sd) ? pc_checkskill(sd, RA_WUGBITE) : skill_get_max(RA_WUGBITE)), tick, SD_ANIMATION);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 50 * skill_lv;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 
    // 	if( target->type != BL_SKILL ) { // Only Hits Invisible Targets
    // 		if (tsc && ((tsc->option&(OPTION_HIDE|OPTION_CLOAK|OPTION_CHASEWALK)) || tsc->getSCE(SC_CAMOUFLAGE) || tsc->getSCE(SC_STEALTHFIELD))) {
    // 			status_change_end(target, SC_CLOAKINGEXCEED);
    // 			WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 		}
    // 		if (tsc && tsc->getSCE(SC__SHADOWFORM) && rnd() % 100 < 100 - tsc->getSCE(SC__SHADOWFORM)->val1 * 10) // [100 - (Skill Level x 10)] %
    // 			status_change_end(target, SC__SHADOWFORM); // Should only end, no damage dealt.
    // 	} else {
    // 		skill_unit *su = BL_CAST(BL_SKILL, target);
    // 		std::shared_ptr<s_skill_unit_group> sg;
    // 
    // 		if (su && (sg = su->group) && skill_get_inf2(sg->skill_id, INF2_ISTRAP)) {
    // 			if( !(sg->unit_id == UNT_USED_TRAPS || (sg->unit_id == UNT_ANKLESNARE && sg->val2 != 0 )) )
    // 			{
    // 				struct item item_tmp;
    // 				memset(&item_tmp,0,sizeof(item_tmp));
    // 				item_tmp.nameid = sg->item_id?sg->item_id:ITEMID_TRAP;
    // 				item_tmp.identify = 1;
    // 				if( item_tmp.nameid )
    // 					map_addflooritem(&item_tmp,1,target->m,target->x,target->y,0,0,0,4,0);
    // 			}
    // 			skill_delunit(su);
    // 		}
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	clif_skill_damage( *src, *src, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	map_foreachinrange(skill_area_sub,src,skill_get_splash(getSkillId(),skill_lv),BL_CHAR|BL_SKILL,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY,skill_castend_damage_id);
    }
}
