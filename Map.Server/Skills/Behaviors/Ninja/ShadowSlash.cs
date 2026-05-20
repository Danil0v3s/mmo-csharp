using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KIRIKAGE — auto-generated stub from
/// <c>src/map/skills/ninja/shadowslash.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ShadowSlash : WeaponSkillImpl
{
    public ShadowSlash() : base(SkillIds.NJ_KIRIKAGE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += -50 + 150 * skill_lv;
    // #else
    // 	base_skillratio += 100 * (skill_lv - 1);
    // #endif
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( !map_flag_gvg2(src->m) && !map_getmapflag(src->m, MF_BATTLEGROUND) )
    // 	{	//You don't move on GVG grounds.
    // 		int16 x, y;
    // 		map_search_freecell(target, 0, &x, &y, 1, 1, 0);
    // 		if (unit_movepos(src, x, y, 0, 0)) {
    // 			clif_blown(src);
    // 		}
    // 	}
    // 	status_change_end(src, SC_HIDING);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
