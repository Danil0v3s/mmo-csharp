using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CHEAL — auto-generated stub from
/// <c>src/map/skills/acolyte/coluceoheal.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ColuceoHeal : SkillImpl
{
    public ColuceoHeal() : base(SkillIds.AB_CHEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 	status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 	map_session_data* dstsd = BL_CAST( BL_PC, target );
    // 
    // 	if( !sd || sd->status.party_id == 0 || flag&1 ) {
    // 		if( sd && tstatus && !battle_check_undead(tstatus->race, tstatus->def_ele) && !tsc->getSCE(SC_BERSERK) ) {
    // 			int32 partycount = (sd->status.party_id ? party_foreachsamemap(party_sub_count, sd, 0) : 0);
    // 
    // 			int32 i = skill_calc_heal(src, target, AL_HEAL, pc_checkskill(sd, AL_HEAL), true);
    // 
    // 			if( partycount > 1 )
    // 				i += (i / 100) * (partycount * 10) / 4;
    // 			if( (dstsd && pc_ismadogear(dstsd)) || status_isimmune(target))
    // 				i = 0; // Should heal by 0 or won't do anything?? in iRO it breaks the healing to members.. [malufett]
    // 
    // 			clif_skill_nodamage(src, *target, getSkillId(), i);
    // 			if( tsc && tsc->getSCE(SC_AKAITSUKI) && i )
    // 				i = ~i + 1;
    // 			status_heal(target, i, 0, 0);
    // 		}
    // 	} else if( sd )
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    }
}
