using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_COLDSLOWER — auto-generated stub from
/// <c>src/map/skills/merchant/coldslower.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ColdSlower : RecursiveDamageSplashSkillImpl
{
    public ColdSlower() : base(SkillIds.NC_COLDSLOWER) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 
    // 	// Status chances are applied officially through a check
    // 	// The skill first trys to give the frozen status to targets that are hit
    // 	sc_start(src, target, SC_FREEZE, 10 * skill_lv, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	if (tsc && !tsc->getSCE(SC_FREEZE)) // If it fails to give the frozen status, it will attempt to give the freezing status
    // 		sc_start(src, target, SC_FREEZING, 20 + skill_lv * 10, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += 200 + 300 * skill_lv;
    // 	RE_LVL_DMOD(150);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Cast center might be relevant later (e.g. for knockback direction)
    // 	skill_area_temp[4] = x;
    // 	skill_area_temp[5] = y;
    // 	int32 i = skill_get_splash(getSkillId(),skill_lv);
    // 	map_foreachinarea(skill_area_sub,src->m,x-i,y-i,x+i,y+i,BL_CHAR|BL_SKILL,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    }
}
