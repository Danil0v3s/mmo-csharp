using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HOWLING_OF_CHUL_HO — auto-generated stub from
/// <c>src/map/skills/summoner/howlingofchulho.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HowlingofChulho : SkillImpl
{
    public HowlingofChulho() : base(SkillIds.SH_HOWLING_OF_CHUL_HO) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 600 + 1050 * skill_lv;
    // 	skillratio += 50 * pc_checkskill(sd, SH_MYSTICAL_CREATURE_MASTERY);
    // 	skillratio += 5 * sstatus->pow;
    // 
    // 	if( pc_checkskill( sd, SH_COMMUNE_WITH_CHUL_HO ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) ){
    // 		skillratio += 100 + 100 * skill_lv;
    // 		skillratio += 50 * pc_checkskill(sd, SH_MYSTICAL_CREATURE_MASTERY);
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	int32 range = skill_get_splash(getSkillId(), skill_lv);
    // 
    // 	if( pc_checkskill( sd, SH_COMMUNE_WITH_CHUL_HO ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) ){
    // 		range += 1;
    // 	}
    // 
    // 	skill_area_temp[0] = 0;
    // 	skill_area_temp[1] = target->id;
    // 	skill_area_temp[2] = 0;
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	map_foreachinrange(skill_area_sub, target, range, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_damage_id);
    }
}
