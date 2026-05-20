using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_LOPE — auto-generated stub from
/// <c>src/map/skills/summoner/lope.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Lope : SkillImpl
{
    public Lope() : base(SkillIds.SU_LOPE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Fails on noteleport maps, except for GvG and BG maps
    // 	if (map_getmapflag(src->m, MF_NOTELEPORT) && !(map_getmapflag(src->m, MF_BATTLEGROUND) || map_flag_gvg2(src->m))) {
    // 		x = src->x;
    // 		y = src->y;
    // 	}
    // 
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    // 	if (!map_count_oncell(src->m, x, y, BL_PC|BL_NPC|BL_MOB, 0) && map_getcell(src->m, x, y, CELL_CHKREACH) && unit_movepos(src, x, y, 1, 0))
    // 		clif_blown(src);
    }
}
