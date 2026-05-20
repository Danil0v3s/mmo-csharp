using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_GRENADES_DROPPING — auto-generated stub from
/// <c>src/map/skills/gunslinger/grenadesdropping.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GrenadesDropping : SkillImpl
{
    public GrenadesDropping() : base(SkillIds.NW_GRENADES_DROPPING) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // uint16 splash = skill_get_splash(getSkillId(), skill_lv);
    // 	uint16 tmpx = rnd_value(x - splash, x + splash);
    // 	uint16 tmpy = rnd_value(y - splash, y + splash);
    // 	skill_unitsetting(src, getSkillId(), skill_lv, tmpx, tmpy, flag);
    // 	for (int32 i = 0; i <= (skill_get_time(getSkillId(), skill_lv) / skill_get_unit_interval(getSkillId())); i++) {
    // 		skill_addtimerskill(src, tick + (t_tick)i * skill_get_unit_interval(getSkillId()), 0, x, y, getSkillId(), skill_lv, 0, flag);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 550 + 850 * skill_lv;
    // 	skillratio += pc_checkskill(sd, NW_GRENADE_MASTERY) * 30;
    // 	skillratio += 5 * sstatus->con;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
