using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_REIKETSUHOU — auto-generated stub from
/// <c>src/map/skills/ninja/coldbloodedcannon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ColdBloodedCannon : SkillImpl
{
    public ColdBloodedCannon() : base(SkillIds.SS_REIKETSUHOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 450 + 950 * skill_lv;
    // 	skillratio += 40 * pc_checkskill( sd, SS_ANTENPOU ) * skill_lv;
    // 	skillratio += 5 * sstatus->spl;
    // 
    // 	if( sc != nullptr && sc->hasSCE( SC_WATER_CHARM_POWER ) ){
    // 		skillratio += 7000;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
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
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skill_mirage_cast(*src, nullptr, SS_ANTENPOU, skill_lv, 0, 0, tick, flag | BCT_WOS);
    // 	if (map_getcell(src->m, x, y, CELL_CHKLANDPROTECTOR)) {
    // 		if (sd != nullptr) {
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		}
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinallarea(skill_area_sub, src->m, x - i, y - i, x + i, y + i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_damage_id);
    }
}
