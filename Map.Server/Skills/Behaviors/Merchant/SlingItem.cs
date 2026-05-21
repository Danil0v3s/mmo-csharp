using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_SLINGITEM — Genetic Sling Item. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/slingitem.cpp</c>.
/// Reads the slinger's equipped ammo, then either melee-strikes via
/// GN_SLINGITEM_RANGEMELEEATK (with pineapple-bomb splash) or applies
/// MaxHP/MaxSP throwable buffs. Item-database lookups are not wired
/// yet, so we land the broadcast and TODO the ammo dispatch.
/// </summary>
public sealed class SlingItem : SkillImpl
{
    public SlingItem() : base(SkillIds.GN_SLINGITEM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: Inspect EQI_AMMO, branch on IG_BOMB vs IG_THROWABLE, and either
        // skill_attack(GN_SLINGITEM_RANGEMELEEATK) or run the throwable item script.
        // The packet is intentionally broadcast twice — that's what triggers the
        // hurl animation client-side.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
