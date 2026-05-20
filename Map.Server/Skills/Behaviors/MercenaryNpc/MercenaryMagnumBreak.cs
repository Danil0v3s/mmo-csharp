using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_MAGNUM — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_magnumbreak.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryMagnumBreak : SkillImpl
{
    public MercenaryMagnumBreak() : base(SkillIds.MS_MAGNUM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if(wd->miscflag == 1)
    // 		base_skillratio += 20 * skill_lv; //Inner 3x3 circle takes 100%+20%*level damage [Playtester]
    // 	else
    // 		base_skillratio += 10 * skill_lv; //Outer 5x5 circle takes 100%+10%*level damage [Playtester]
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( flag&1 ) {
    // 		// For players, damage depends on distance, so add it to flag if it is > 1
    // 		// Cannot hit hidden targets
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag|SD_ANIMATION|(sd?distance_bl(src, target):0));
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = 0;
    // 	map_foreachinshootrange(skill_area_sub, src, skill_get_splash(getSkillId(), skill_lv), BL_SKILL|BL_CHAR,
    // 		src,getSkillId(),skill_lv,tick, flag|BCT_ENEMY|1, skill_castend_damage_id);
    // 	clif_skill_nodamage(src, *src,getSkillId(),skill_lv);
    // 	// Initiate 20% of your damage becomes fire element.
    // #ifdef RENEWAL
    // 	sc_start4(src,src,SC_SUB_WEAPONPROPERTY,100,ELE_FIRE,20,getSkillId(),0,skill_get_time2(getSkillId(), skill_lv));
    // #else
    // 	sc_start4(src,src,SC_WATK_ELEMENT,100,ELE_FIRE,20,0,0,skill_get_time2(getSkillId(), skill_lv));
    // #endif
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // hit_rate += hit_rate * 10 * skill_lv / 100;
    return hitRate;
    }
}
