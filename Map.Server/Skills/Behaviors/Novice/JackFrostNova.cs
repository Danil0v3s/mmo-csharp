using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_JACK_FROST_NOVA — auto-generated stub from
/// <c>src/map/skills/novice/jackfrostnova.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class JackFrostNova : SkillImpl
{
    public JackFrostNova() : base(SkillIds.HN_JACK_FROST_NOVA) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, skill_get_sc(getSkillId()), 100, 0, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if (mflag & SKILL_ALTDMG_FLAG) {
    // 		// Initial damage
    // 		skillratio += -100 + 200 * skill_lv;
    // 		skillratio += 2 * sstatus->spl;
    // 	} else {
    // 		// Explosion damage
    // 		skillratio += -100 + 400 + 500 * skill_lv;
    // 		skillratio += 4 * sstatus->spl;
    // 	}
    // 	skillratio += pc_checkskill(sd, HN_SELFSTUDY_SOCERY) * 3 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	// After RE_LVL_DMOD calculation, HN_SELFSTUDY_SOCERY amplifies the skill ratio of HN_JACK_FROST_NOVA (explosion damage) by (skill level)%
    // 	if (!(mflag & SKILL_ALTDMG_FLAG))
    // 		skillratio += skillratio * pc_checkskill(sd, HN_SELFSTUDY_SOCERY) / 100;
    // 	// SC_RULEBREAK increases the skill ratio after HN_SELFSTUDY_SOCERY
    // 	if (sc && sc->getSCE(SC_RULEBREAK))
    // 		skillratio += skillratio * 70 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( map_getcell(src->m, x, y, CELL_CHKLANDPROTECTOR) ) {
    // 		if( sd != nullptr ){
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		}
    // 
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	int32 splash = skill_get_splash(getSkillId(), skill_lv);
    // 
    // 	map_foreachinarea(skill_area_sub, src->m, x - splash, y - splash, x + splash, y + splash, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | SKILL_ALTDMG_FLAG | 1, skill_castend_damage_id);
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, flag);
    // 
    // 	for (int32 i = 1; i <= (skill_get_time(getSkillId(), skill_lv) / skill_get_unit_interval(getSkillId())); i++) {
    // 		skill_addtimerskill(src, tick + (t_tick)i*skill_get_unit_interval(getSkillId()), 0, x, y, getSkillId(), skill_lv, 0, flag);
    // 	}
    }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (dmg.miscflag & SKILL_ALTDMG_FLAG) {
    // 		// Initial damage
    // 		dmg.div_ = 1;
    // 	}
    }
}
