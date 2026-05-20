using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FLICKER — auto-generated stub from
/// <c>src/map/skills/gunslinger/flicker.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Flicker : SkillImpl
{
    public Flicker() : base(SkillIds.RL_FLICKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd) {
    // 		sd->flicker = true;
    // 		skill_area_temp[1] = 0;
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		// Detonate RL_B_TRAP
    // 		if (pc_checkskill(sd, RL_B_TRAP)) {
    // 			map_foreachinallrange(skill_bind_trap, src, AREA_SIZE, BL_SKILL, src);
    // 		}
    // 		// Detonate RL_H_MINE
    // 		if (int32 mine_lv = pc_checkskill(sd, RL_H_MINE)) {
    // 			map_foreachinallrange(skill_area_sub, src, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, RL_H_MINE, mine_lv, tick, flag | BCT_ENEMY | SD_SPLASH, skill_castend_damage_id);
    // 		}
    // 		sd->flicker = false;
    // 	}
    }
}
