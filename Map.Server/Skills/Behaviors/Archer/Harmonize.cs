using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_HARMONIZE — auto-generated stub from
/// <c>src/map/skills/archer/harmonize.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Harmonize : SkillImpl
{
    public Harmonize() : base(SkillIds.MI_HARMONIZE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	int32 duration = skill_get_time(getSkillId(), skill_lv);
    // 
    // 	if( src != target ) {
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv, sc_start(src, src, type, 100, skill_lv, duration));
    // 	}
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start(src, target, type, 100, skill_lv, duration));
    }
}
