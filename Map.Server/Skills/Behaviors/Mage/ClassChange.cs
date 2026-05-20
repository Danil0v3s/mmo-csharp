using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CLASSCHANGE — auto-generated stub from
/// <c>src/map/skills/mage/classchange.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ClassChange : SkillImpl
{
    public ClassChange() : base(SkillIds.SA_CLASSCHANGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 	int32 i = 0;
    // 
    // 	if (dstmd)
    // 	{
    // 		int32 class_;
    // 
    // 		if ( sd && status_has_mode(&dstmd->status,MD_STATUSIMMUNE) ) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 		class_ = mob_get_random_id(MOBG_CLASSCHANGE, RMF_DB_RATE, 0);
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 		mob_class_change(dstmd,class_);
    // 		if( tsc && status_has_mode(&dstmd->status,MD_STATUSIMMUNE) ) {
    // 			const enum sc_type scs[] = { SC_QUAGMIRE, SC_PROVOKE, SC_ROKISWEIL, SC_GRAVITATION, SC_SUITON, SC_STRIPWEAPON, SC_STRIPSHIELD, SC_STRIPARMOR, SC_STRIPHELM, SC_BLADESTOP };
    // 			for (i = SC_COMMON_MIN; i <= SC_COMMON_MAX; i++)
    // 				if (tsc->getSCE(i)) status_change_end(target, (sc_type)i);
    // 			for (i = 0; i < ARRAYLENGTH(scs); i++)
    // 				if (tsc->getSCE(scs[i])) status_change_end(target, scs[i]);
    // 		}
    // 	}
    }
}
