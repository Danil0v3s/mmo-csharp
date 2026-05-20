using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SHOCKWAVE — auto-generated stub from
/// <c>src/map/skills/archer/shockwavetrap.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ShockwaveTrap : SkillImpl
{
    public ShockwaveTrap() : base(SkillIds.HT_SHOCKWAVE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag |= 1;
    // 
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_percent_damage(src, target, 0, -(15*skill_lv+5), false);
    }
}
