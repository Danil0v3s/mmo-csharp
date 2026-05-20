using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_THE_VIGILANTE_AT_NIGHT — auto-generated stub from
/// <c>src/map/skills/gunslinger/thevigilanteatnight.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TheVigilanteAtNight : SkillImpl
{
    public TheVigilanteAtNight() : base(SkillIds.NW_THE_VIGILANTE_AT_NIGHT) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	if (sd != nullptr && sd->weapontype1 == W_GATLING)
    // 		dmg.div_ += 3;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* sc = status_get_sc(src);
    // 
    // 	int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	skill_area_temp[0] = 0;
    // 	skill_area_temp[1] = target->id;
    // 	skill_area_temp[2] = 0;
    // 
    // 	if (sd && sd->weapontype1 == W_GATLING) {
    // 		i = 5; // 11x11
    // 		clif_skill_nodamage(src, *target, NW_THE_VIGILANTE_AT_NIGHT_GUN_GATLING, skill_lv);
    // 	} else
    // 		clif_skill_nodamage(src, *target, NW_THE_VIGILANTE_AT_NIGHT_GUN_SHOTGUN, skill_lv);
    // 	map_foreachinrange(skill_area_sub, target, i, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	if (sd && sd->weapontype1 == W_GATLING) {
    // 		skillratio += -100 + 300 * skill_lv;
    // 		if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 			skillratio += sc->getSCE(SC_INTENSIVE_AIM_COUNT)->val1 * 100 * skill_lv;
    // 	} else {
    // 		skillratio += -100 + 800 + 700 * skill_lv;
    // 		if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 			skillratio += sc->getSCE(SC_INTENSIVE_AIM_COUNT)->val1 * 200 * skill_lv;
    // 	}
    // 	skillratio += 5 * sstatus->con;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
