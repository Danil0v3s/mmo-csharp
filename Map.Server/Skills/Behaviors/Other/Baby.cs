using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_BABY — auto-generated stub from
/// <c>src/map/skills/other/baby.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Baby : SkillImpl
{
    public Baby() : base(SkillIds.WE_BABY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr)
    // 		return;
    // 
    // 	map_session_data* f_sd = pc_get_father(sd);
    // 	map_session_data* m_sd = pc_get_mother(sd);
    // 
    // 	// Neither was found
    // 	if (f_sd == nullptr && m_sd == nullptr) {
    // 		clif_skill_fail(*sd, getSkillId());
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	// Not in same party
    // 	// TODO : no check if sd->status.party_id == 0 ?
    // 	if (sd->status.party_id != 0 && (f_sd == nullptr || sd->status.party_id != f_sd->status.party_id) && (m_sd == nullptr || sd->status.party_id != m_sd->status.party_id)) {
    // 		clif_skill_fail(*sd, getSkillId());
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	// Not in same screen
    // 	if ((f_sd == nullptr || !check_distance_bl(sd, f_sd, AREA_SIZE)) && (m_sd == nullptr || !check_distance_bl(sd, m_sd, AREA_SIZE))) {
    // 		clif_skill_fail(*sd, getSkillId());
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	status_change_start(src, target, SC_STUN, 10000, skill_lv, 0, 0, 0, skill_get_time2(getSkillId(), skill_lv), SCSTART_NORATEDEF);
    // 
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (f_sd != nullptr)
    // 		sc_start(src, f_sd, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	if (m_sd != nullptr)
    // 		sc_start(src, m_sd, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }
}
