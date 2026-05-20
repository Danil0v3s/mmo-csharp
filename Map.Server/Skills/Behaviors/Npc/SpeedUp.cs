using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_SPEEDUP — auto-generated stub from
/// <c>src/map/skills/npc/speedup.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpeedUp : SkillImpl
{
    public SpeedUp() : base(SkillIds.NPC_SPEEDUP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	if (md != nullptr) {
    // 		// Officially, trickcasting continues as long as there are more than 700ms left
    // 		int32 trickstop = (MOB_SKILL_INTERVAL * 7) / 10;
    // 		if (DIFF_TICK(md->trickcasting, tick) >= trickstop) {
    // 			// This skill directly modifies a monster's base speed value
    // 			md->base_status->speed = std::max(md->base_status->speed - 250, MIN_WALK_SPEED);
    // 			// Need to recalc speed based on new base value
    // 			status_calc_bl(md, { SCB_SPEED });
    // 			// We use skills only on each full cell, to fix the inaccuracy we do this on last move interval
    // 			if (DIFF_TICK(md->trickcasting, tick) < trickstop + MOB_SKILL_INTERVAL)
    // 				md->last_skillcheck = tick + 100;
    // 		}
    // 		else {
    // 			// Synchronize skill usage
    // 			md->last_skillcheck = md->trickcasting;
    // 			// Causes monster to stop and get ready for next alchemist skill
    // 			md->trickcasting = 0;
    // 			md->state.can_escape = 0;
    // 		}
    // 	}
    }
}
