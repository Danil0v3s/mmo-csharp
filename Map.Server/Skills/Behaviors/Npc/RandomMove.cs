using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_RANDOMMOVE — auto-generated stub from
/// <c>src/map/skills/npc/randommove.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RandomMove : SkillImpl
{
    public RandomMove() : base(SkillIds.NPC_RANDOMMOVE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	if (md != nullptr) {
    // 		// This skill creates fake casting state where a monster moves while showing a cast bar
    // 		int32 tricktime = MOB_SKILL_INTERVAL * 3;
    // 		md->trickcasting = tick + tricktime;
    // 		clif_skillcasting(*src, src, 0, 0, getSkillId(), skill_lv, ELE_FIRE, tricktime + MOB_SKILL_INTERVAL / 2);
    // 		// Monster cannot be stopped while moving
    // 		md->state.can_escape = 1;
    // 		// Move up to 8 cells
    // 		unit_escape(md, target, 8, 3);
    // 	}
    }
}
