using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_POISONINGWEAPON — auto-generated stub from
/// <c>src/map/skills/thief/poisoningweapon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PoisoningWeapon : SkillImpl
{
    public PoisoningWeapon() : base(SkillIds.GC_POISONINGWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sd ) {
    // 		clif_poison_list( *sd, skill_lv );
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
