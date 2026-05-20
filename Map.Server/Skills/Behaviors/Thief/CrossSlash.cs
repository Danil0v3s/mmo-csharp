using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_CROSS_SLASH — auto-generated stub from
/// <c>src/map/skills/thief/crossslash.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CrossSlash : RecursiveDamageSplashSkillImpl
{
    public CrossSlash() : base(SkillIds.SHC_CROSS_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 300 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_SHADOW_EXCEED ) ) {
    // 		skillratio += 60 * skill_lv;
    // 		skillratio += 2 * sstatus->pow;
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
