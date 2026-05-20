using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_LULLABY_DEEPSLEEP — auto-generated stub from
/// <c>src/map/skills/archer/deepsleeplullaby.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DeepSleepLullaby : SkillImpl
{
    public DeepSleepLullaby() : base(SkillIds.WM_LULLABY_DEEPSLEEP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (flag&1) {
    // 		int32 rate = 4 * skill_lv + (sd ? pc_checkskill(sd, WM_LESSON) * 2 : 0) + status_get_lv(src) / 15 + (sd ? sd->status.job_level / 5 : 0);
    // 		int32 duration = skill_get_time(getSkillId(), skill_lv) - (status_get_base_status(target)->int_ * 50 + status_get_lv(target) * 50); // Duration reduction for Deep Sleep Lullaby is doubled
    // 
    // 		sc_start(src, target, type, rate, skill_lv, duration);
    // 	} else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 	}
    }
}
