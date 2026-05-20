using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_DESTRUCTIVE_HURRICANE — auto-generated stub from
/// <c>src/map/skills/mage/destructivehurricane.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DestructiveHurricane : RecursiveDamageSplashSkillImpl
{
    public DestructiveHurricane() : base(SkillIds.AG_DESTRUCTIVE_HURRICANE) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(&src);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_CLIMAX) && sc->getSCE(SC_CLIMAX)->val1 == 2)
    // 		dmg.blewcount = 2;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	// Targets hit are dealt a additional hit through Climax.
    // 	if (sc && sc->getSCE(SC_CLIMAX) && sc->getSCE(SC_CLIMAX)->val1 == 1)
    // 		skill_castend_damage_id(src, target, AG_DESTRUCTIVE_HURRICANE_CLIMAX, skill_lv, tick, SD_LEVEL|SD_ANIMATION);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 600 + 2850 * skill_lv + 5 * sstatus->spl;
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
    // 			splash_size = 9; // 19x19
    // 		}
    // 
    // 		skill_area_temp[1] = 0;
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 		if (climax_lv == 4) // Buff for caster instead of damage AoE.
    // 			sc_start(src, target, type, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 		else {
    // 			if (climax_lv == 1) // Display extra animation for the additional hit cast.
    // 				clif_skill_nodamage(src, *target, AG_DESTRUCTIVE_HURRICANE_CLIMAX, skill_lv);
    // 
    // 			map_foreachinrange(skill_area_sub, target, splash_size, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 		}
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    }
}
