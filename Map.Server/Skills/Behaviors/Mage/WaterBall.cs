using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_WATERBALL — auto-generated stub from
/// <c>src/map/skills/mage/waterball.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WaterBall : SkillImpl
{
    public WaterBall() : base(SkillIds.WZ_WATERBALL) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Deploy waterball cells, these are used and turned into waterballs via the timerskill
    // 	skill_unitsetting(src, getSkillId(), skill_lv, src->x, src->y, 0);
    // 	skill_addtimerskill(src, tick, target->id, src->x, src->y, getSkillId(), skill_lv, 0, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 30 * skill_lv;
    return baseRatio;
    }
}
