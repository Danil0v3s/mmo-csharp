using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_HASTY_FIRE_IN_THE_HOLE — auto-generated stub from
/// <c>src/map/skills/gunslinger/hastyfireinthehole.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HastyFireInTheHole : WeaponSkillImpl
{
    public HastyFireInTheHole() : base(SkillIds.NW_HASTY_FIRE_IN_THE_HOLE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	if (flag & 1){
    // 		i++;
    // 	}
    // 	if (flag & 2){
    // 		i++;
    // 	}
    // 	map_foreachinallarea(skill_area_sub,
    // 		src->m, x - i, y - i, x + i, y + i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1,
    // 		skill_castend_damage_id);
    // 	if (!(flag & 1)) {
    // 		skill_addtimerskill(src, tick + 300, 0, x, y, getSkillId(), skill_lv, 0, flag | 1 | SKILL_NOCONSUME_REQ);
    // 		skill_addtimerskill(src, tick + 600, 0, x, y, getSkillId(), skill_lv, 0, flag | 3 | SKILL_NOCONSUME_REQ);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 1500 + 1500 * skill_lv;
    // 	skillratio += pc_checkskill(sd, NW_GRENADE_MASTERY) * 20;
    // 	skillratio += 5 * sstatus->con;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
