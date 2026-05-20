using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_EXPOSION_BLASTER — auto-generated stub from
/// <c>src/map/skills/acolyte/explosionblaster.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ExplosionBlaster : RecursiveDamageSplashSkillImpl
{
    public ExplosionBlaster() : base(SkillIds.IQ_EXPOSION_BLASTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change* tsc = status_get_sc(target);
    // 
    // 	skillratio += -100 + 450 + 2600 * skill_lv;
    // 	skillratio += 10 * sstatus->pow;
    // 
    // 	if (tsc != nullptr && tsc->getSCE(SC_HOLY_OIL)) {
    // 		skillratio += 950 * skill_lv;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
