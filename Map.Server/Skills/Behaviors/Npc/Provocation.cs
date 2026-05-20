using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_PROVOCATION — auto-generated stub from
/// <c>src/map/skills/npc/provocation.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Provocation : SkillImpl
{
    public Provocation() : base(SkillIds.NPC_PROVOCATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	if (md) mob_unlocktarget(md, tick);
    }
}
