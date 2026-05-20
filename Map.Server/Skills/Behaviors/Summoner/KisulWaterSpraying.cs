using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_KI_SUL_WATER_SPRAYING — auto-generated stub from
/// <c>src/map/skills/summoner/kisulwaterspraying.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class KisulWaterSpraying : SkillImpl
{
    public KisulWaterSpraying() : base(SkillIds.SH_KI_SUL_WATER_SPRAYING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		// TODO: verify on official server, if this should be moved into skill_calc_heal
    // 		int32 heal = 500 * skill_lv + status_get_int(src) * 5;
    // 		heal += pc_checkskill(sd, SH_MYSTICAL_CREATURE_MASTERY) * 100;
    // 
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_KI_SUL ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) ){
    // 			heal += 250 * skill_lv;
    // 			heal += pc_checkskill(sd, SH_MYSTICAL_CREATURE_MASTERY) * 50;
    // 		}
    // 		heal = heal * (100 + status_get_crt(src)) * status_get_lv(src) / 10000;
    // 		status_heal(target, heal, 0, 0, 0);
    // 		clif_skill_nodamage(nullptr, *target, AL_HEAL, heal);
    // 	}
    // 	else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		int32 range = skill_get_splash(getSkillId(), skill_lv);
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_KI_SUL ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) )
    // 			range += 2;
    // 		party_foreachsamemap(skill_area_sub, sd, range, src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    // 	}
    }
}
