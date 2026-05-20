using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FALLEN_ANGEL — auto-generated stub from
/// <c>src/map/skills/gunslinger/fallenangel.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FallenAngel : SkillImpl
{
    public FallenAngel() : base(SkillIds.RL_FALLEN_ANGEL) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (unit_movepos(src, x, y, 1, 1)) {
    // 		clif_snap(src, src->x, src->y);
    // 		sc_start(src, src, type, 100, getSkillId(), skill_get_time(getSkillId(), skill_lv));
    // 	} else if (sd != nullptr) {
    // 		clif_skill_fail(*sd, getSkillId());
    // 	}
    }
}
