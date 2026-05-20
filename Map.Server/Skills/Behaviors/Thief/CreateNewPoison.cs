using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CREATENEWPOISON — auto-generated stub from
/// <c>src/map/skills/thief/createnewpoison.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CreateNewPoison : SkillImpl
{
    public CreateNewPoison() : base(SkillIds.GC_CREATENEWPOISON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sd )
    // 	{
    // 		clif_skill_produce_mix_list( *sd, getSkillId(), 25 );
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
