using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_DETONATOR — Ranger Detonator. Manual port of
/// <c>rathena-fork/src/map/skills/archer/detonator.cpp</c>.
/// Detonates all of the caster's own traps in splash. Trap-unit
/// iteration via map_foreachinallarea(skill_detonator) not surfaced
/// — broadcast only.
/// </summary>
public sealed class Detonator : SkillImpl
{
    public Detonator() : base(SkillIds.RA_DETONATOR) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: walk BL_SKILL units in splash, fire skill_detonator on each — needs BL_SKILL splash enumerator in ctx.
    }
}
