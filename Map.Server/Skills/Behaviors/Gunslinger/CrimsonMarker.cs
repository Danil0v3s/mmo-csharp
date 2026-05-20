using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_C_MARKER — auto-generated stub from
/// <c>src/map/skills/gunslinger/crimsonmarker.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CrimsonMarker : SkillImpl
{
    public CrimsonMarker() : base(SkillIds.RL_C_MARKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 	status_change* tsc = status_get_sc(target);
    // 	status_change_entry* tsce = (tsc && type != SC_NONE) ? tsc->getSCE(type) : nullptr;
    // 	int32 i;
    // 
    // 	if (sd) {
    // 		// If marked by someone else remove it
    // 		if (tsce && tsce->val2 != src->id) {
    // 			status_change_end(target, type);
    // 		}
    // 
    // 		// Check if marked before
    // 		ARR_FIND(0, MAX_SKILL_CRIMSON_MARKER, i, sd->c_marker[i] == target->id);
    // 		if (i == MAX_SKILL_CRIMSON_MARKER) {
    // 			// Find empty slot
    // 			ARR_FIND(0, MAX_SKILL_CRIMSON_MARKER, i, !sd->c_marker[i]);
    // 			if (i == MAX_SKILL_CRIMSON_MARKER) {
    // 				clif_skill_fail(*sd, getSkillId());
    // 				return;
    // 			}
    // 		}
    // 
    // 		sd->c_marker[i] = target->id;
    // 		status_change_start(src, target, type, 10000, skill_lv, src->id, 0, 0, skill_get_time(getSkillId(), skill_lv), SCSTART_NOAVOID | SCSTART_NOTICKDEF | SCSTART_NORATEDEF);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	} else {
    // 		// If mob casts this, at least SC_C_MARKER as debuff
    // 		status_change_start(src, target, type, 10000, skill_lv, src->id, 0, 0, skill_get_time(getSkillId(), skill_lv), SCSTART_NOAVOID | SCSTART_NOTICKDEF | SCSTART_NORATEDEF);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
