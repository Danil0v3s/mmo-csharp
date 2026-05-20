using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_HUNDREDSPEAR — auto-generated stub from
/// <c>src/map/skills/swordman/hundredspear.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HundredSpear : RecursiveDamageSplashSkillImpl
{
    public HundredSpear() : base(SkillIds.RK_HUNDREDSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 600 + 200 * skill_lv;
    // 	if (sd)
    // 		skillratio += 50 * pc_checkskill(sd,LK_SPIRALPIERCE);
    // 	if (sc) {
    // 		if( sc->getSCE( SC_DRAGONIC_AURA ) ){
    // 			skillratio += sc->getSCE( SC_DRAGONIC_AURA )->val1 * 160;
    // 		}
    // 
    // 		if (sc->getSCE(SC_CHARGINGPIERCE_COUNT) && sc->getSCE(SC_CHARGINGPIERCE_COUNT)->val1 >= 10)
    // 			skillratio *= 2;
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
