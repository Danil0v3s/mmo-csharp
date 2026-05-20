using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_SELFPROVOKE — auto-generated stub from
/// <c>src/map/skills/swordman/selfprovoke.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ProvokeSelf : SkillImpl
{
    public ProvokeSelf() : base(SkillIds.SM_SELFPROVOKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	status_data *tstatus = status_get_status_data(*bl);
    // 	map_session_data *sd = BL_CAST(BL_PC, src);
    // 	mob_data *dstmd = BL_CAST(BL_MOB, bl);
    // 
    // 	if (status_has_mode(tstatus, MD_STATUSIMMUNE) || battle_check_undead(tstatus->race, tstatus->def_ele))
    // 	{
    // 		return;
    // 	}
    // 
    // 	int32 success = sc_start(src, bl, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	if (!success)
    // 	{
    // 		if (sd)
    // 			clif_skill_fail(*sd, getSkillId());
    // 		return;
    // 	}
    // 	clif_skill_nodamage(src, *bl, SM_PROVOKE, skill_lv, success != 0);
    // 	unit_skillcastcancel(bl, 2);
    // 
    // 	if (dstmd)
    // 	{
    // 		dstmd->state.provoke_flag = src->id;
    // 		mob_target(dstmd, src, skill_get_range2(src, getSkillId(), skill_lv, true));
    // 	}
    // 	// Provoke can cause Coma even though it's a nodamage skill
    // 	if (sd && battle_check_coma(*sd, *bl, BF_MISC))
    // 		status_change_start(src, bl, SC_COMA, 10000, skill_lv, 0, src->id, 0, 0, SCSTART_NONE);
    }
}
