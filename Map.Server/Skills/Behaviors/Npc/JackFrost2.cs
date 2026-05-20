using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_JACKFROST — auto-generated stub from
/// <c>src/map/skills/npc/jackfrost2.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class JackFrost2 : RecursiveDamageSplashSkillImpl
{
    public JackFrost2() : base(SkillIds.NPC_JACKFROST) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_FREEZE,200,skill_lv,skill_get_time(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	if (tsc && tsc->getSCE(SC_FREEZING)) {
    // 		skillratio += 900 + 300 * skill_lv;
    // 		RE_LVL_DMOD(100);
    // 	} else {
    // 		skillratio += 400 + 100 * skill_lv;
    // 		RE_LVL_DMOD(150);
    // 	}
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	map_foreachinrange(skill_area_sub,target,skill_get_splash(getSkillId(),skill_lv),BL_CHAR|BL_SKILL,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    }
}
