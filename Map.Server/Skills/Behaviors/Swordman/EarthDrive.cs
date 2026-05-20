using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_EARTHDRIVE — auto-generated stub from
/// <c>src/map/skills/swordman/earthdrive.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EarthDrive : RecursiveDamageSplashSkillImpl
{
    public EarthDrive() : base(SkillIds.LG_EARTHDRIVE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 dummy = 1;
    // 
    // 	clif_skill_damage( *src, *bl,tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	int32 i = skill_get_splash(getSkillId(),skill_lv);
    // 	map_foreachinallarea(skill_cell_overlap, src->m, src->x-i, src->y-i, src->x+i, src->y+i, BL_SKILL, getSkillId(), &dummy, src);
    // 	map_foreachinrange(skill_area_sub, bl,i,BL_CHAR,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 380 * skill_lv + sstatus->str + sstatus->vit; // !TODO: What's the STR/VIT bonus?
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_SHIELD_POWER ) ){
    // 		skillratio += skill_lv * 37 * pc_checkskill( sd, IG_SHIELD_MASTERY );
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
