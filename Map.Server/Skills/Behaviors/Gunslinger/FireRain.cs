using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FIRE_RAIN — auto-generated stub from
/// <c>src/map/skills/gunslinger/firerain.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FireRain : SkillImpl
{
    public FireRain() : base(SkillIds.RL_FIRE_RAIN) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 wave = skill_lv + 5;
    // 	int32 dir = map_calc_dir(src, x, y);
    // 	int32 sx = src->x;
    // 	int32 sy = src->y;
    // 
    // 	x = src->x;
    // 	y = src->y;
    // 
    // 	for (int32 w = 0; w <= wave; ++w) {
    // 		switch (dir) {
    // 			case DIR_NORTH:
    // 			case DIR_NORTHWEST:
    // 			case DIR_NORTHEAST:
    // 				sy = y + w;
    // 				break;
    // 			case DIR_WEST:
    // 				sx = x - w;
    // 				break;
    // 			case DIR_SOUTHWEST:
    // 			case DIR_SOUTH:
    // 			case DIR_SOUTHEAST:
    // 				sy = y - w;
    // 				break;
    // 			case DIR_EAST:
    // 				sx = x + w;
    // 				break;
    // 		}
    // 		skill_addtimerskill(src, gettick() + (80 * w), 0, sx, sy, getSkillId(), skill_lv, dir, flag);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 3500 + 300 * skill_lv;
    return baseRatio;
    }
}
