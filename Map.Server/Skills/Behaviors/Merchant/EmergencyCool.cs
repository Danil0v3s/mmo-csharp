using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_EMERGENCYCOOL — Mechanic Emergency Cool. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/emergencycool.cpp</c>.
/// Reduces madogear overheat by 45/75/105 based on cooling item used.
/// pc_overheat helper not wired — broadcast only.
/// </summary>
public sealed class EmergencyCool : SkillImpl
{
    public EmergencyCool() : base(SkillIds.NC_EMERGENCYCOOL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: pc_overheat helper not ported; selecting the -45/-75/-105 tier
        // also requires consuming the matching cooling-item from inventory, which
        // depends on the produce / item-consume pipeline that isn't wired yet.
    }
}
