using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_SPEARSTAB — auto-generated stub from
/// <c>src/map/skills/swordman/spearstab.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpearStab : SkillImpl
{
    public SpearStab() : base(SkillIds.KN_SPEARSTAB) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // dmg.blewcount = 0;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if(flag&1) {
    // 		if (target->id==skill_area_temp[1])
    // 			return;
    // 		if (skill_attack(BF_WEAPON,src,src,target,getSkillId(), skill_lv, tick, SD_ANIMATION))
    // 			skill_blown(src,target,skill_area_temp[2],-1,BLOWN_NONE);
    // 	} else {
    // 		int32 x=target->x,y=target->y,i,dir;
    // 		dir = map_calc_dir(target,src->x,src->y);
    // 		skill_area_temp[1] = target->id;
    // 		skill_area_temp[2] = skill_get_blewcount(getSkillId(),skill_lv);
    // 		// all the enemies between the caster and the target are hit, as well as the target
    // 		if (skill_attack(BF_WEAPON,src,src,target, getSkillId(),skill_lv,tick,0))
    // 			skill_blown(src,target,skill_area_temp[2],-1,BLOWN_NONE);
    // 		for (i=0;i<4;i++) {
    // 			map_foreachincell(skill_area_sub,target->m,x,y,BL_CHAR,
    // 				src, getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    // 			x += dirx[dir];
    // 			y += diry[dir];
    // 		}
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 20 * skill_lv;
    return baseRatio;
    }
}
