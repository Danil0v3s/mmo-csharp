using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_WINKCHARM — auto-generated stub from
/// <c>src/map/skills/archer/winkofcharm.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WinkofCharm : SkillImpl
{
    public WinkofCharm() : base(SkillIds.DC_WINKCHARM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 
    // 	if( dstsd ) {
    // #ifdef RENEWAL
    // 		// In Renewal it causes Confusion and Hallucination to 100% base chance
    // 		sc_start(src, target, SC_CONFUSION, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		sc_start(src, target, SC_HALLUCINATION, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // #else
    // 		// In Pre-Renewal it only causes Wink Charm, if Confusion was successfully started
    // 		if (sc_start(src, target, SC_CONFUSION, 10, skill_lv, skill_get_time(getSkillId(), skill_lv)))
    // 			sc_start(src, target, type, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // #endif
    // 	} else
    // 	if( dstmd )
    // 	{
    // 		// For monsters it causes Wink Charm with a chance depending on the level difference
    // 		if (sc_start2(src, target, type, (status_get_lv(src) - status_get_lv(target)) + 40, skill_lv, src->id, skill_get_time2(getSkillId(), skill_lv))) {
    // 			// This triggers a 0 damage event and might make the monster switch target to caster
    // 			battle_damage(src, target, 0, 1, skill_lv, 0, ATK_DEF, BF_WEAPON|BF_LONG|BF_NORMAL, true, tick, false);
    // 		}
    // 	}
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    }
}
