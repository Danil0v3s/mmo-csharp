using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_MEDIALE_VOTUM — auto-generated stub from
/// <c>src/map/skills/acolyte/medialevotum.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MedialeVotum : StatusSkillImpl
{
    public MedialeVotum() : base(SkillIds.CD_MEDIALE_VOTUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (flag & 1) {
    // 		if (sd == nullptr || sd->status.party_id == 0 || (flag & 2)) {
    // 			int32 heal_amount = skill_calc_heal(src, target, getSkillId(), skill_lv, 1);
    // 
    // 			clif_skill_nodamage(nullptr, *target, AL_HEAL, heal_amount);
    // 			status_heal(target, heal_amount, 0, 0);
    // 		} else if (sd)
    // 			party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag | BCT_PARTY | 3, skill_castend_nodamage_id);
    // 	} else {
    // 		StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // 	}
    }
}
