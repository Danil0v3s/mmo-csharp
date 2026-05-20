using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_MAKINGARROW — auto-generated stub from
/// <c>src/map/skills/archer/makingarrow.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MakingArrow : SkillImpl
{
    public MakingArrow() : base(SkillIds.AC_MAKINGARROW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd != nullptr)
    // 	{
    // 		clif_arrow_create_list(*sd);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
