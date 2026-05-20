using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_MISSION_BOMBARD — auto-generated stub from
/// <c>src/map/skills/gunslinger/missionbombard.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MissionBombard : WeaponSkillImpl
{
    public MissionBombard() : base(SkillIds.NW_MISSION_BOMBARD) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinarea(skill_area_sub, src->m, x - i, y - i, x + i, y + i, BL_CHAR | BL_SKILL, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SKILL_ALTDMG_FLAG | 1, skill_castend_damage_id);
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, flag);
    // 
    // 	for (i = 1; i <= (skill_get_time(getSkillId(), skill_lv) / skill_get_unit_interval(getSkillId())); i++) {
    // 		skill_addtimerskill(src, tick + (t_tick)i * skill_get_unit_interval(getSkillId()), 0, x, y, getSkillId(), skill_lv, 0, flag);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	if (wd->miscflag & SKILL_ALTDMG_FLAG) {
    // 		skillratio += -100 + 5000 + 1800 * skill_lv;
    // 		skillratio += pc_checkskill(sd, NW_GRENADE_MASTERY) * 100;
    // 	}
    // 	else {
    // 		skillratio += -100 + 800 + 200 * skill_lv;
    // 		skillratio += pc_checkskill(sd, NW_GRENADE_MASTERY) * 30;
    // 	}
    // 	skillratio += 5 * sstatus->con;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
