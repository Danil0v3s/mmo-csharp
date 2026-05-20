using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SOA_TALISMAN_OF_SOUL_STEALING — auto-generated stub from
/// <c>src/map/skills/taekwon/talismanofsoulstealing.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TalismanOfSoulStealing : SkillImpl
{
    public TalismanOfSoulStealing() : base(SkillIds.SOA_TALISMAN_OF_SOUL_STEALING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 500 + 1250 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SOA_TALISMAN_MASTERY) * 7 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SOA_SOUL_MASTERY) * 7 * skill_lv;
    // 	skillratio += 3 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	if( target->type != BL_SKILL ){
    // 		int32 sp = (100 + status_get_lv(src) / 50) * skill_lv;
    // 
    // 		status_heal(src, 0, sp, 0, 0);
    // 		clif_skill_nodamage( src, *src, getSkillId(), sp );
    // 	}
    }
}
