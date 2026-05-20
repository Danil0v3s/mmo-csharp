using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_HIGHJUMP — auto-generated stub from
/// <c>src/map/skills/taekwon/highjump.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HighJump : SkillImpl
{
    public HighJump() : base(SkillIds.TK_HIGHJUMP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 x, y, dir = unit_getdir(src);
    // 	map_data *mapdata = map_getmapdata(src->m);
    // 
    // 	// Fails on noteleport maps, except for GvG and BG maps [Skotlex]
    // 	if (mapdata->getMapFlag(MF_NOTELEPORT) && !(mapdata->getMapFlag(MF_BATTLEGROUND) || mapdata_flag_gvg(mapdata))) {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		return;
    // 	} else if (dir % 2) {
    // 		// Diagonal
    // 		x = src->x + dirx[dir] * (skill_lv * 4) / 3;
    // 		y = src->y + diry[dir] * (skill_lv * 4) / 3;
    // 	} else {
    // 		x = src->x + dirx[dir] * skill_lv * 2;
    // 		y = src->y + diry[dir] * skill_lv * 2;
    // 	}
    // 
    // 	int32 x1 = x + dirx[dir];
    // 	int32 y1 = y + diry[dir];
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	if (!map_count_oncell(src->m, x, y, BL_PC | BL_NPC | BL_MOB, 0) && map_getcell(src->m, x, y, CELL_CHKREACH) &&
    // 	    !map_count_oncell(src->m, x1, y1, BL_PC | BL_NPC | BL_MOB, 0) && map_getcell(src->m, x1, y1, CELL_CHKREACH) &&
    // 	    unit_movepos(src, x, y, 1, 0))
    // 		clif_blown(src);
    }
}
