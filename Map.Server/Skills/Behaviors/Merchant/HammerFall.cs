using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_HAMMERFALL — auto-generated stub from
/// <c>src/map/skills/merchant/hammerfall.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HammerFall : SkillImpl
{
    public HammerFall() : base(SkillIds.BS_HAMMERFALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_addtimerskill(src, tick+1000, target->id, 0, 0, getSkillId(), skill_lv, min(20+10*skill_lv, 50+5*skill_lv), flag);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinallarea(skill_area_sub,
    // 		src->m, x-i, y-i, x+i, y+i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|2,
    // 		skill_castend_nodamage_id);
    }
}
