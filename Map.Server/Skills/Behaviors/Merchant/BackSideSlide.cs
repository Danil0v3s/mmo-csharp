using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_B_SIDESLIDE — auto-generated stub from
/// <c>src/map/skills/merchant/backsideslide.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BackSideSlide : SkillImpl
{
    public BackSideSlide() : base(SkillIds.NC_B_SIDESLIDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // uint8 dir = unit_getdir(src);
    // 	skill_blown(src,target,skill_get_blewcount(getSkillId(),skill_lv),dir,BLOWN_IGNORE_NO_KNOCKBACK);
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    }
}
