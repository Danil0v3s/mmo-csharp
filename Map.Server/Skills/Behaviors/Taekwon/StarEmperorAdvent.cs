using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_STAREMPEROR — auto-generated stub from
/// <c>src/map/skills/taekwon/staremperoradvent.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StarEmperorAdvent : RecursiveDamageSplashSkillImpl
{
    public StarEmperorAdvent() : base(SkillIds.SJ_STAREMPEROR) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_SILENCE, 50 + 10 * skill_lv, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 700 + 200 * skill_lv;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sc && sc->getSCE(SC_DIMENSION)) {
    // 		if (sd) {
    // 			// Remove old shields if any exist.
    // 			pc_delspiritball(sd, sd->spiritball, 0);
    // 			sc_start2(src, target, SC_DIMENSION1, 100, skill_lv, status_get_max_sp(src), skill_get_time2(SJ_BOOKOFDIMENSION, 1));
    // 			sc_start2(src, target, SC_DIMENSION2, 100, skill_lv, status_get_max_sp(src), skill_get_time2(SJ_BOOKOFDIMENSION, 1));
    // 		}
    // 		status_change_end(src, SC_DIMENSION);
    // 	}
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    }
}
