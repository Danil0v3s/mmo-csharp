using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_AUTOSHADOWSPELL — auto-generated stub from
/// <c>src/map/skills/thief/autoshadowspell.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AutoShadowSpell : SkillImpl
{
    public AutoShadowSpell() : base(SkillIds.SC_AUTOSHADOWSPELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		if( (sd->reproduceskill_idx > 0 && sd->status.skill[sd->reproduceskill_idx].id) ||
    // 			(sd->cloneskill_idx > 0 && sd->status.skill[sd->cloneskill_idx].id) )
    // 		{
    // 			sc_start(src,src,SC_STOP,100,skill_lv,INFINITE_TICK);// The skill_lv is stored in val1 used in skill_select_menu to determine the used skill lvl [Xazax]
    // 			clif_autoshadowspell_list( *sd );
    // 			clif_skill_nodamage(src,*target,getSkillId(),1);
    // 		}
    // 		else
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL_IMITATION_SKILL_NONE );
    // 	}
    }
}
