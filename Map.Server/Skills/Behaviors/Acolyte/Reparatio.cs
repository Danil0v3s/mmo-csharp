using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_REPARATIO — auto-generated stub from
/// <c>src/map/skills/acolyte/reparatio.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Reparatio : SkillImpl
{
    public Reparatio() : base(SkillIds.CD_REPARATIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_data* tstatus = status_get_status_data(*target);
    // 
    // 	if (target->type != BL_PC) { // Only works on players.
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 
    // 	int32 heal_amount = 0;
    // 
    // 	if (!status_isimmune(target))
    // 		heal_amount = tstatus->max_hp;
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	clif_skill_nodamage(nullptr, *target, AL_HEAL, heal_amount);
    // 	status_heal(target, heal_amount, 0, 0);
    }
}
