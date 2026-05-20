using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_REBIRTH — auto-generated stub from
/// <c>src/map/skills/npc/rebirth.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Rebirth : SkillImpl
{
    public Rebirth() : base(SkillIds.NPC_REBIRTH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	if( md && md->state.rebirth )
    // 		return; // only works once
    // 	sc_start(src,target,skill_get_sc(getSkillId()),100,skill_lv,INFINITE_TICK);
    }
}
