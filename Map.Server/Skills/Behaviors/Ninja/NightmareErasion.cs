using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_AKUMUKESU — auto-generated stub from
/// <c>src/map/skills/ninja/nightmareerasion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NightmareErasion : SkillImpl
{
    public NightmareErasion() : base(SkillIds.SS_AKUMUKESU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1) {
    // 		status_change_end(target, SC_NIGHTMARE);
    // 	} else {
    // 		int32 range = skill_get_splash( getSkillId(), skill_lv );
    // 
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 		map_foreachinrange( skill_area_sub, target, range, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_nodamage_id );
    // 	}
    }
}
