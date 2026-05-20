using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SOA_TALISMAN_OF_BLACK_TORTOISE — auto-generated stub from
/// <c>src/map/skills/taekwon/talismanofblacktortoise.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TalismanOfBlackTortoise : SkillImpl
{
    public TalismanOfBlackTortoise() : base(SkillIds.SOA_TALISMAN_OF_BLACK_TORTOISE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 2150 + 1600 * skill_lv;
    // 	skillratio += pc_checkskill(sd, SOA_TALISMAN_MASTERY) * 15 * skill_lv;
    // 	skillratio += 5 * sstatus->spl;
    // 	if (sc != nullptr && sc->getSCE(SC_T_FIFTH_GOD) != nullptr)
    // 		skillratio += 150 + 500 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	if (sc != nullptr && sc->getSCE(SC_T_THIRD_GOD) != nullptr){
    // 		sc_start(src, src, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	}
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
