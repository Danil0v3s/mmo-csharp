using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_FIRE_EXPANSION — Genetic Fire Expansion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fireexpansion.cpp</c>.
///
/// <para>Consumes a nearby Demonic Fire unit and transforms it per
/// skill level (1: extended duration, 2: detonate splash, 3: smoke
/// powder, 4: tear gas, 5: acid demonstration cascade). Full
/// skill_unit lookup + variant dispatch isn't wired here — broadcast
/// only.</para>
/// </summary>
public sealed class FireExpansion : SkillImpl
{
    public FireExpansion() : base(SkillIds.GN_FIRE_EXPANSION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: locate GN_DEMONIC_FIRE unit near (x, y) and apply per-level transform.
    }
}
