using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SP_CURSEEXPLOSION — auto-generated stub from
/// <c>src/map/skills/taekwon/curseexplosion.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CurseExplosion : RecursiveDamageSplashSkillImpl
{
    public CurseExplosion() : base(SkillIds.SP_CURSEEXPLOSION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	if (tsc && tsc->getSCE(SC_SOULCURSE))
    // 		skillratio += -100 + 1200 + 300 * skill_lv;
    // 	else
    // 		skillratio += -100 + 400 + 100 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
