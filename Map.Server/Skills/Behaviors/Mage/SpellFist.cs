using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SPELLFIST — auto-generated stub from
/// <c>src/map/skills/mage/spellfist.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpellFist : SkillImpl
{
    public SpellFist() : base(SkillIds.SO_SPELLFIST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	unit_skillcastcancel(src, 1);
    // 
    // 	if (sd) {
    // 		if (sd->skill_id_old != 0 && sd->skill_lv_old != 0) {
    // 			sc_start4(src, src, skill_get_sc(getSkillId()), 100, skill_lv, sd->skill_id_old, sd->skill_lv_old, 0, skill_get_time(getSkillId(), skill_lv));
    // 		}
    // 		sd->skill_id_old = sd->skill_lv_old = 0;
    // 	}
    }
}
