using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_INVISIBLE — auto-generated stub from
/// <c>src/map/skills/npc/invisible.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Invisible : SkillImpl
{
    public Invisible() : base(SkillIds.NPC_INVISIBLE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Have val4 passed as 6 is for "infinite cloak" (do not end on attack/skill use).
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,
    // 		sc_start4(src,target,skill_get_sc(getSkillId()),100,skill_lv,0,0,6,skill_get_time(getSkillId(),skill_lv)));
    }
}
