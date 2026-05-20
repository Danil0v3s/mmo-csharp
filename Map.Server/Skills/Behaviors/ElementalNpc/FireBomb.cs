using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_BOMB — auto-generated stub from
/// <c>src/map/skills/elemental/firebomb.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FireBomb : SkillImpl
{
    public FireBomb() : base(SkillIds.EL_FIRE_BOMB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 400;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( flag&1 )
    // 		skill_attack(skill_get_type(EL_FIRE_BOMB_ATK),src,src,target,EL_FIRE_BOMB_ATK,skill_lv,tick,flag);
    // 	else {
    // 		int32 i = skill_get_splash(getSkillId(),skill_lv);
    // 		clif_skill_nodamage(src,*battle_get_master(src),getSkillId(),skill_lv);
    // 		clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 		if( rnd()%100 < 30 )
    // 			map_foreachinrange(skill_area_sub,target,i,BL_CHAR,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    // 		else
    // 			skill_attack(skill_get_type(getSkillId()),src,src,target,getSkillId(),skill_lv,tick,flag);
    // 	}
    }


}
