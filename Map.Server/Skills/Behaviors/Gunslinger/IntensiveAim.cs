using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_INTENSIVE_AIM — auto-generated stub from
/// <c>src/map/skills/gunslinger/intensiveaim.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class IntensiveAim : SkillImpl
{
    public IntensiveAim() : base(SkillIds.NW_INTENSIVE_AIM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // enum sc_type type = skill_get_sc(getSkillId());
    // 	status_change* tsc = status_get_sc(target);
    // 
    // 	if (tsc && tsc->getSCE(type)) {
    // 		status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    // 		status_change_end(target, type);
    // 	} else {
    // 		status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    // 		sc_start(src, target, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    }
}
