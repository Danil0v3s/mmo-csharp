using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_SPIRAL_SHOOTING — auto-generated stub from
/// <c>src/map/skills/gunslinger/spiralshooting.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpiralShooting : SkillImpl
{
    public SpiralShooting() : base(SkillIds.NW_SPIRAL_SHOOTING) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	if (sd != nullptr && sd->weapontype1 == W_GRENADE)
    // 		dmg.div_ += 1;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* sc = status_get_sc(src);
    // 
    // 	if (flag & 1) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	} else {
    // 		int32 splash = skill_get_splash(getSkillId(), skill_lv);
    // 
    // 		if (sd && sd->weapontype1 == W_GRENADE)
    // 			splash += 2;
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		map_foreachinrange(skill_area_sub, target, splash, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 		if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 			status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 1200 + 1700 * skill_lv;
    // 	skillratio += 5 * sstatus->con;
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		skillratio += sc->getSCE(SC_INTENSIVE_AIM_COUNT)->val1 * 150 * skill_lv;
    // 	if (sd && sd->weapontype1 == W_RIFLE)
    // 		skillratio += 200 + 1100 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
