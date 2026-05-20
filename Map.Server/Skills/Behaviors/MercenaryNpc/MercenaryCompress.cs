using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_COMPRESS — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_compress.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryCompress : SkillImpl
{
    public MercenaryCompress() : base(SkillIds.MER_COMPRESS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_end(target, SC_BLEEDING);
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    }
}
