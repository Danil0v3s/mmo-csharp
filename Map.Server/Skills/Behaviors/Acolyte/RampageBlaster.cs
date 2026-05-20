using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_RAMPAGEBLASTER — auto-generated stub from
/// <c>src/map/skills/acolyte/rampageblaster.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RampageBlaster : RecursiveDamageSplashSkillImpl
{
    public RampageBlaster() : base(SkillIds.SR_RAMPAGEBLASTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change* sc = status_get_sc(src);
    // 	const status_change* tsc = status_get_sc(target);
    // 
    // 	if (tsc && tsc->getSCE(SC_EARTHSHAKER)) {
    // 		skillratio += 1400 + 550 * skill_lv;
    // 		RE_LVL_DMOD(120);
    // 	} else {
    // 		skillratio += 900 + 350 * skill_lv;
    // 		RE_LVL_DMOD(150);
    // 	}
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_GT_CHANGE))
    // 		skillratio += skillratio * 30 / 100;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }
}
