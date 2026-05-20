using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_MARINE_FESTIVAL_OF_KI_SUL — auto-generated stub from
/// <c>src/map/skills/summoner/marinefestivalofkisul.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MarineFestivalofKisul : SkillImpl
{
    public MarineFestivalofKisul() : base(SkillIds.SH_MARINE_FESTIVAL_OF_KI_SUL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		int32 time = skill_get_time(getSkillId(), skill_lv);
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_KI_SUL ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) )
    // 			time *= 2;
    // 		sc_start(src, target, type, 100, skill_lv, time);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    // 	else {
    // 		int32 range = skill_get_splash(getSkillId(), skill_lv);
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_KI_SUL ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) )
    // 			range += 2;
    // 		party_foreachsamemap(skill_area_sub, sd, range, src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    // 	}
    }
}
