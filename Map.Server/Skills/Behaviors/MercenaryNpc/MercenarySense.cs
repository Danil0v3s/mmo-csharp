using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_ESTIMATION — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_sense.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenarySense : SkillImpl
{
    public MercenarySense() : base(SkillIds.MER_ESTIMATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	s_mercenary_data* mer = BL_CAST(BL_MER, src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if( !mer )
    // 		return;
    // 	sd = mer->master;
    // 	if( sd == nullptr )
    // 		return;
    // 	if( dstsd )
    // 	{ // Fail on Players
    // 		clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 
    // 	if (dstmd != nullptr)
    // 		clif_skill_estimation( *sd, *dstmd );
    // 	sd = nullptr;
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    }
}
