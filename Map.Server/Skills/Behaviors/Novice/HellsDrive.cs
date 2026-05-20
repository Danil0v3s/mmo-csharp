using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_HELLS_DRIVE — auto-generated stub from
/// <c>src/map/skills/novice/hellsdrive.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HellsDrive : SkillImpl
{
    public HellsDrive() : base(SkillIds.HN_HELLS_DRIVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 1700 + 900 * skill_lv;
    // 	skillratio += pc_checkskill(sd, HN_SELFSTUDY_SOCERY) * 4 * skill_lv;
    // 	skillratio += 3 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    // 	// After RE_LVL_DMOD calculation, HN_SELFSTUDY_SOCERY amplifies the skill ratio of HN_HELLS_DRIVE by (skill level)%
    // 	skillratio += skillratio * pc_checkskill(sd, HN_SELFSTUDY_SOCERY) / 100;
    // 	// SC_RULEBREAK increases the skill ratio after HN_SELFSTUDY_SOCERY
    // 	if (sc && sc->getSCE(SC_RULEBREAK))
    // 		skillratio += skillratio * 70 / 100;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_damage_id);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
