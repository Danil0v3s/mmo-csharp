using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FEINTBOMB — auto-generated stub from
/// <c>src/map/skills/thief/feintbomb.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FeintBomb : WeaponSkillImpl
{
    public FeintBomb() : base(SkillIds.SC_FEINTBOMB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + (skill_lv + 1) * sstatus->dex / 2 * ((sd) ? sd->status.job_level / 10 : 1);
    // 	RE_LVL_DMOD(120);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	std::shared_ptr<s_skill_unit_group> group = skill_unitsetting(src,getSkillId(),skill_lv,x,y,0); // Set bomb on current Position
    // 
    // 	if( group == nullptr || group->unit == nullptr ) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	map_foreachinallrange(unit_changetarget, src, AREA_SIZE, BL_MOB, src, group->unit); // Release all targets against the caster
    // 	skill_blown(src, src, skill_get_blewcount(getSkillId(), skill_lv), unit_getdir(src), BLOWN_IGNORE_NO_KNOCKBACK); // Don't stop the caster from backsliding if special_state.no_knockback is active
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv, false);
    // 	sc_start(src, src, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }
}
