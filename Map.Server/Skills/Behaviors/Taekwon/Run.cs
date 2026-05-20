using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_RUN — auto-generated stub from
/// <c>src/map/skills/taekwon/run.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Run : SkillImpl
{
    public Run() : base(SkillIds.TK_RUN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	status_change *tsc = status_get_sc(target);
    // 	status_change_entry *tsce = (tsc && type != SC_NONE) ? tsc->getSCE(type) : nullptr;
    // 	map_session_data *sd = BL_CAST(BL_PC, src);
    // 
    // 	if (tsce) {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, status_change_end(target, type));
    // 		return;
    // 	}
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start4(src, target, type, 100, skill_lv, unit_getdir(target), 0, 0, 0));
    // 	if (sd) // If the client receives a skill-use packet inmediately before a walkok packet, it will discard the walk packet! [Skotlex]
    // 		clif_walkok(*sd); // So aegis has to resend the walk ok.
    }
}
