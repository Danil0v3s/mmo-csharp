using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_RAID — auto-generated stub from
/// <c>src/map/skills/thief/sightlessmind.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SightlessMind : RecursiveDamageSplashSkillImpl
{
    public SightlessMind() : base(SkillIds.RG_RAID) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_STUN,(10+3*skill_lv),skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 	sc_start(src,target,SC_BLIND,(10+3*skill_lv),skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // #ifdef RENEWAL
    // 	sc_start(src, target, SC_RAID, 100, skill_lv, 10000); // Hardcoded to 10 seconds since Duration1 and Duration2 are used
    // #endif
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += -100 + 50 + skill_lv * 150;
    // #else
    // 	base_skillratio += 40 * skill_lv;
    // #endif
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = 0;
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	map_foreachinrange(skill_area_sub, target,
    // 		skill_get_splash(getSkillId(), skill_lv), BL_CHAR|BL_SKILL,
    // 		src,getSkillId(),skill_lv,tick, flag|BCT_ENEMY|1,
    // 		skill_castend_damage_id);
    // 	status_change_end(src, SC_HIDING);
    }
}
