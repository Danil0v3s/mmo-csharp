using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SP_SOULEXPLOSION — auto-generated stub from
/// <c>src/map/skills/taekwon/soulexplosion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulExplosion : SkillImpl
{
    public SoulExplosion() : base(SkillIds.SP_SOULEXPLOSION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Remove soul link when hit.
    // 	status_change_end(target, SC_SPIRIT);
    // 	status_change_end(target, SC_SOULGOLEM);
    // 	status_change_end(target, SC_SOULSHADOW);
    // 	status_change_end(target, SC_SOULFALCON);
    // 	status_change_end(target, SC_SOULFAIRY);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 	status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if (!(tsc && (tsc->getSCE(SC_SPIRIT) || tsc->getSCE(SC_SOULGOLEM) || tsc->getSCE(SC_SOULSHADOW) || tsc->getSCE(SC_SOULFALCON) || tsc->getSCE(SC_SOULFAIRY))) || tstatus->hp < 10 * tstatus->max_hp / 100) { // Requires target to have a soul link and more then 10% of MaxHP.
    // 		// With this skill requiring a soul link, and the target to have more then 10% if MaxHP, I wonder
    // 		// if the cooldown still happens after it fails. Need a confirm. [Rytech] 
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 
    // 	skill_attack(BF_MISC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
