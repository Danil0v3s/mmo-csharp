using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// OB_OBOROGENSOU — auto-generated stub from
/// <c>src/map/skills/ninja/moonlightfantasy.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MoonlightFantasy : StatusSkillImpl
{
    public MoonlightFantasy() : base(SkillIds.OB_OBOROGENSOU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	// This skill does not work on monsters. And it does not work on status immune monsters.
    // 	if( sd && ( target->type == BL_MOB || status_bl_has_mode(target,MD_STATUSIMMUNE) ) ){ 
    // 		clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    }
}
