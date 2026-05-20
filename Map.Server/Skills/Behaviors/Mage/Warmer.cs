using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_WARMER — auto-generated stub from
/// <c>src/map/skills/mage/warmer.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Warmer : SkillImpl
{
    public Warmer() : base(SkillIds.SO_WARMER) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag |= 8;
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
