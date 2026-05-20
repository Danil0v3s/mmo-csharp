using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_CRYSTAL_IMPACT — auto-generated stub from
/// <c>src/map/skills/mage/crystalimpact.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CrystalImpact : RecursiveDamageSplashSkillImpl
{
    public CrystalImpact() : base(SkillIds.AG_CRYSTAL_IMPACT) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(&src);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_CLIMAX) && sc->getSCE(SC_CLIMAX)->val1 == 2)
    // 		dmg.div_ = 2;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Targets hit are dealt aftershock damage.
    // 	skill_castend_damage_id(src, target, AG_CRYSTAL_IMPACT_ATK, skill_lv, tick, SD_LEVEL);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 250 + 1300 * skill_lv + 5 * sstatus->spl;
    // 	// (climax buff applied with pc_skillatk_bonus)
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (flag&1) { // Buff from Crystal Impact with level 1 Climax.
    // 		sc_start(src, target, type, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	} else {
    // 		uint16 climax_lv = 0, splash_size = skill_get_splash(getSkillId(), skill_lv);
    // 
    // 		if (sc && sc->getSCE(SC_CLIMAX))
    // 			climax_lv = sc->getSCE(SC_CLIMAX)->val1;
    // 
    // 		if (climax_lv == 5) { // Adjusts splash AoE size depending on skill.
    // 			splash_size = 7; // 15x15
    // 		}
    // 
    // 		skill_area_temp[1] = 0;
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 		if (climax_lv == 1) // Buffs the caster and allies instead of doing damage AoE.
    // 			map_foreachinrange(skill_area_sub, target, splash_size, BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ALLY|SD_SPLASH|1, skill_castend_nodamage_id);
    // 		else
    // 			map_foreachinrange(skill_area_sub, target, splash_size, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	}
    }


}
