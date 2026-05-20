using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_EARTHSTRAIN — auto-generated stub from
/// <c>src/map/skills/mage/earthstrain.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EarthStrain : SkillImpl
{
    public EarthStrain() : base(SkillIds.WL_EARTHSTRAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 1000 + 600 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 w, wave = skill_lv + 4, dir = map_calc_dir(src,x,y);
    // 	int32 sx = x = src->x, sy = y = src->y; // Store first caster's location to avoid glitch on unit setting
    // 
    // 	for( w = 1; w <= wave; w++ )
    // 	{
    // 		switch( dir ){
    // 			case 0: case 1: case 7: sy = y + w; break;
    // 			case 3: case 4: case 5: sy = y - w; break;
    // 			case 2: sx = x - w; break;
    // 			case 6: sx = x + w; break;
    // 		}
    // 		skill_addtimerskill(src,gettick() + (140 * w),0,sx,sy,getSkillId(),skill_lv,dir,flag&2);
    // 	}
    }
}
