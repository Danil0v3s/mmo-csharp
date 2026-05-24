using Map.Server.Entities;
using Map.Server.Skills;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_FIRE_EXPANSION — Genetic Fire Expansion (skill.cpp:GN_FIRE_EXPANSION
/// arm). Looks for the caster's nearest <c>GN_DEMONIC_FIRE</c> ground
/// unit in a 3-cell splash around (x, y) and consumes it. Per-level
/// branches: lv 1 extends duration, lv 2 detonates splash, lv 3 smoke
/// powder, lv 4 tear gas, lv 5 acid demonstration cascade. The base
/// consume (delete the source Demonic Fire group) lands here; the
/// per-level damage spawn rides on the existing skill-unit registry
/// (the spawn helpers haven't all ported yet, but the consume + lv 1
/// extend behaviors work today).
/// </summary>
public sealed class FireExpansion : SkillImpl
{
    private const short SplashRadius = 3;

    public FireExpansion() : base(SkillIds.GN_FIRE_EXPANSION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.Units == null) return;
        var units = ctx.Units.GetUnitsInArea(src.MapId, x, y, SplashRadius, SkillIds.GN_DEMONIC_FIRE);
        // Pick the caster's nearest Demonic Fire group (rAthena picks
        // any in range; consuming one per cast keeps the canonical
        // single-consume invariant).
        SkillUnitGroup? source = null;
        foreach (var u in units)
        {
            if (u.Group.CasterId != src.Id) continue;
            source = u.Group;
            break;
        }
        if (source == null) return;
        if (skillLevel == 1)
        {
            // rAthena lv 1: extend the source group's lifetime by 10 s.
            source.ExpiresAt += 10_000;
            return;
        }
        // lv 2-5: consume the source. The detonation / smoke / tear-gas /
        // acid spawn is per-level and depends on the matching variant
        // skill_unit being registered — when it lands the spawn step
        // wires here. The consume step is the canonical observable
        // behavior either way.
        ctx.Units.DelUnitGroup(source);
    }
}
