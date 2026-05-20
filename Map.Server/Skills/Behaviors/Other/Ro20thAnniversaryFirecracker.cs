using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_EVENT_20TH_ANNIVERSARY — auto-generated stub from
/// <c>src/map/skills/other/ro20thanniversaryfirecracker.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Ro20thAnniversaryFirecracker : SkillImpl
{
    public Ro20thAnniversaryFirecracker() : base(SkillIds.ALL_EVENT_20TH_ANNIVERSARY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    }
}
