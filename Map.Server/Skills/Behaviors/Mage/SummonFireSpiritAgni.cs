using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_AGNI — auto-generated stub from
/// <c>src/map/skills/mage/summonfirespiritagni.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SummonFireSpiritAgni : SkillImpl
{
    public SummonFireSpiritAgni() : base(SkillIds.SO_SUMMON_AGNI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		int32 elemental_class = skill_get_elemental_type(getSkillId(),skill_lv);
    // 
    // 		// Remove previous elemental first.
    // 		if( sd->ed )
    // 			elemental_delete(sd->ed);
    // 
    // 		// Summoning the new one.
    // 		if( !elemental_create(sd,elemental_class,skill_get_time(getSkillId(),skill_lv)) ) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
