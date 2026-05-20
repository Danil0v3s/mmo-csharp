using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// HP_BASILICA — auto-generated stub from
/// <c>src/map/skills/acolyte/basilica.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Basilica : StatusSkillImpl
{
    public Basilica() : base(SkillIds.HP_BASILICA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // #endif
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifndef RENEWAL
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( status_change *sc = status_get_sc(src); sc && sc->getSCE(SC_BASILICA) ) {
    // 		status_change_end(src, SC_BASILICA); // Cancel Basilica and return so requirement isn't consumed again
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	if( map_getcell(src->m, x, y, CELL_CHKLANDPROTECTOR) ) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	
    // 	// Create Basilica
    // 	skill_clear_unitgroup(src);
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // 	flag|=1;
    // #endif
    }
}
