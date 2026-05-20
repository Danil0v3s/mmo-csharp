using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_POWEROFFLOCK — auto-generated stub from
/// <c>src/map/skills/summoner/powerofflock.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PowerofFlock : SkillImpl
{
    public PowerofFlock() : base(SkillIds.SU_POWEROFFLOCK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag&1) {
    // 		sc_start(src, target, SC_FEAR, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		sc_start(src, target, SC_FREEZE, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv)); //! TODO: What's the duration?
    // 	} else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		if (battle_config.skill_wall_check)
    // 			map_foreachinshootrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 		else
    // 			map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 	}
    }
}
