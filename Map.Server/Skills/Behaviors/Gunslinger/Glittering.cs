using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_GLITTERING — auto-generated stub from
/// <c>src/map/skills/gunslinger/glittering.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Glittering : SkillImpl
{
    public Glittering() : base(SkillIds.GS_GLITTERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd) {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		if (rnd() % 100 < (20 + 10 * skill_lv))
    // 			pc_addspiritball(sd, skill_get_time(getSkillId(), skill_lv), 10);
    // 		else if (sd->spiritball > 0 && !pc_checkskill(sd, RL_RICHS_COIN))
    // 			pc_delspiritball(sd, 1, 0);
    // 	}
    }
}
