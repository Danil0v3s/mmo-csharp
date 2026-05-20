using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_IMPOSITIO — auto-generated stub from
/// <c>src/map/skills/acolyte/impositiomanus.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ImpositioManus : SkillImpl
{
    public ImpositioManus() : base(SkillIds.PR_IMPOSITIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 
    // 		// Animations don't play when outside visible range
    // 		if (check_distance_bl(src, target, AREA_SIZE))
    // 			clif_skill_nodamage(target, *target, getSkillId(), skill_lv);
    // 
    // 		sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}
    // 	else if (sd)
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag | BCT_PARTY | 1, skill_castend_nodamage_id);
    }
}
