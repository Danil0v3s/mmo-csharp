using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_KI_SUL_RAMPAGE — auto-generated stub from
/// <c>src/map/skills/summoner/kisulrampage.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class KisulRampage : SkillImpl
{
    public KisulRampage() : base(SkillIds.SH_KI_SUL_RAMPAGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( flag&2 ){
    // 		if( src == target ){
    // 			return;
    // 		}
    // 
    // 		int64 ap = 2;
    // 
    // 		if( flag&4 ){
    // 			ap += 4;
    // 		}
    // 
    // 		status_heal( target, 0, 0, ap, 0 );
    // 	}else if( flag&1 ){
    // 		map_session_data* sd = BL_CAST(BL_PC, src);
    // 		status_change *sc = status_get_sc(src);
    // 		int32 range = skill_get_splash( getSkillId(), skill_lv );
    // 
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_KI_SUL ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) ){
    // 			range += 2;
    // 			// Set a flag for AP increase
    // 			flag |= 4;
    // 		}
    // 
    // 		clif_skill_nodamage( src, *target, getSkillId(), 0 );
    // 		map_foreachinrange( skill_area_sub, target, range, BL_CHAR, target, getSkillId(), skill_lv, tick, flag|BCT_PARTY|2, skill_castend_nodamage_id );
    // 	}else{
    // 		// No party check required
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}
    }
}
