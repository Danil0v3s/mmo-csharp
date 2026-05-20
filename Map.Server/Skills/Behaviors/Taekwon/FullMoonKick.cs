using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_FULLMOONKICK — auto-generated stub from
/// <c>src/map/skills/taekwon/fullmoonkick.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FullMoonKick : RecursiveDamageSplashSkillImpl
{
    public FullMoonKick() : base(SkillIds.SJ_FULLMOONKICK) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_BLIND, 15 + 5 * skill_lv, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += 1000 + 100 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if (sc && sc->getSCE(SC_LIGHTOFMOON))
    // 		skillratio += skillratio * sc->getSCE(SC_LIGHTOFMOON)->val2 / 100;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }
}
