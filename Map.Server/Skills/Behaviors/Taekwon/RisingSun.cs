using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SKE_RISING_SUN — auto-generated stub from
/// <c>src/map/skills/taekwon/risingsun.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RisingSun : SkillImpl
{
    public RisingSun() : base(SkillIds.SKE_RISING_SUN) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* sc = status_get_sc(src);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 
    // 	if ( sc == nullptr || ( sc->getSCE( SC_RISING_SUN ) == nullptr && sc->getSCE( SC_NOON_SUN ) == nullptr && sc->getSCE( SC_SUNSET_SUN ) == nullptr ) ){
    // 		sc_start(src, src, SC_RISING_SUN, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}else if( sc->getSCE( SC_NOON_SUN ) == nullptr && sc->getSCE( SC_SUNSET_SUN ) == nullptr ){
    // 		sc_start(src, src, SC_NOON_SUN, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}else if( sc->getSCE( SC_SUNSET_SUN ) == nullptr ){
    // 		sc_start(src, src, SC_SUNSET_SUN, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 500 + 600 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SKE_SKY_MASTERY) * 5 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
