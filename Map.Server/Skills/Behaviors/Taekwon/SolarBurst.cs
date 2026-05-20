using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_SOLARBURST — auto-generated stub from
/// <c>src/map/skills/taekwon/solarburst.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SolarBurst : RecursiveDamageSplashSkillImpl
{
    public SolarBurst() : base(SkillIds.SJ_SOLARBURST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += 900 + 220 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if (sc && sc->getSCE(SC_LIGHTOFSUN))
    // 		skillratio += skillratio * sc->getSCE(SC_LIGHTOFSUN)->val2 / 100;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }
}
