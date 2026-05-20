using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_NAPALM_VULCAN_STRIKE — auto-generated stub from
/// <c>src/map/skills/novice/napalmvulcanstrike.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NapalmVulcanStrike : SkillImpl
{
    public NapalmVulcanStrike() : base(SkillIds.HN_NAPALM_VULCAN_STRIKE) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_CURSE,5*skill_lv,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 350 + 650 * skill_lv;
    // 	skillratio += pc_checkskill(sd, HN_SELFSTUDY_SOCERY) * 4 * skill_lv;
    // 	skillratio += 3 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    // 	// After RE_LVL_DMOD calculation, HN_SELFSTUDY_SOCERY amplifies the skill ratio of HN_NAPALM_VULCAN_STRIKE by (2x skill level)%
    // 	skillratio += skillratio * 2 * pc_checkskill(sd, HN_SELFSTUDY_SOCERY) / 100;
    // 	// SC_RULEBREAK increases the skill ratio after HN_SELFSTUDY_SOCERY
    // 	if (sc && sc->getSCE(SC_RULEBREAK))
    // 		skillratio += skillratio * 40 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	} else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	}
    }
}
