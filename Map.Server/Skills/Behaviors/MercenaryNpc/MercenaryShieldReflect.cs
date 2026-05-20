using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_REFLECTSHIELD — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_shieldreflect.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryShieldReflect : StatusSkillImpl
{
    public MercenaryShieldReflect() : base(SkillIds.MS_REFLECTSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (tsc && tsc->getSCE(SC_DARKCROW)) { // SC_DARKCROW prevents using reflecting skills
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		return;
    // 	}
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    }
}
