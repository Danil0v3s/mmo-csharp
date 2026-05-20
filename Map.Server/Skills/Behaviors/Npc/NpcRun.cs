using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_RUN — auto-generated stub from
/// <c>src/map/skills/npc/npcrun.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcRun : SkillImpl
{
    public NpcRun() : base(SkillIds.NPC_RUN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	if (md) {
    // 		block_list* tbl = map_id2bl(md->target_id);
    // 
    // 		if (tbl) {
    // 			md->state.can_escape = 1;
    // 			mob_unlocktarget(md, tick);
    // 			// Official distance is 7, if level > 1, distance = level
    // 			t_tick time = unit_escape(src, tbl, skill_lv > 1 ? skill_lv : 7, 3);
    // 
    // 			if (time) {
    // 				// Need to set state here as it's not set otherwise
    // 				mob_setstate(*md, MSS_WALK);
    // 				// Set AI to inactive for the duration of this movement
    // 				md->next_thinktime = tick + time;
    // 			}
    // 		}
    // 	}
    }
}
