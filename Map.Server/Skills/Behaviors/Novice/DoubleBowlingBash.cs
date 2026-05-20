using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_DOUBLEBOWLINGBASH — auto-generated stub from
/// <c>src/map/skills/novice/doublebowlingbash.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DoubleBowlingBash : SkillImpl
{
    public DoubleBowlingBash() : base(SkillIds.HN_DOUBLEBOWLINGBASH) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (dmg.miscflag > 1) {
    // 		dmg.div_ += min(4, dmg.miscflag);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 250 + 400 * skill_lv;
    // 	skillratio += pc_checkskill(sd, HN_SELFSTUDY_TATICS) * 3 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, skill_area_temp[0] & 0xFFF);
    // 	} else {
    // 		int32 splash = skill_get_splash(getSkillId(), skill_lv);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		skill_area_temp[0] = map_foreachinallrange(skill_area_sub, target, splash, BL_CHAR, src, getSkillId(), skill_lv, tick, BCT_ENEMY, skill_area_sub_count);
    // 		map_foreachinrange(skill_area_sub, target, splash, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 		sc_start(src, src, SC_HNNOWEAPON, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	}
    }
}
