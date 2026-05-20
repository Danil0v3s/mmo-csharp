using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_PARTYFLEE — auto-generated stub from
/// <c>src/map/skills/other/partyflee.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PartyFlee : StatusSkillImpl
{
    public PartyFlee() : base(SkillIds.ALL_PARTYFLEE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd  && !(flag&1) ) {
    // 		if( !sd->status.party_id ) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    // 	} else
    // 		StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    }
}
