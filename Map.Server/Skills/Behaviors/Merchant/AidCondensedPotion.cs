using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_SLIMPITCHER — Crusader Aid Condensed Potion (Slim Pitcher).
/// Manual port of <c>rathena-fork/src/map/skills/merchant/aidcondensedpotion.cpp</c>.
/// Ground-targeted party-wide heal. Inventory-script driven; the full
/// potion-script path (potion_hp / potion_sp) isn't wired here, so we
/// land the cast frame as a no-op for now.
/// </summary>
public sealed class AidCondensedPotion : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public AidCondensedPotion() : base(SkillIds.CR_SLIMPITCHER) { }

    public AidCondensedPotion(IStatusOpsService? statusOps = null) : base(SkillIds.CR_SLIMPITCHER)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // First-slice heal magnitude: 100*lv. The full rAthena
        // formula reads the consumed potion's inventory-script
        // potion_hp/potion_sp values; deferred until the potion-script
        // pipeline ports (P2.2.a SkillProductionService row).
        _statusOps?.Heal(target, 100 * skillLevel, 0, 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: ground-target Slim Pitcher splashes its heal across
        // every same-map partymate. Same heal magnitude as the targeted
        // form (100*lv first-slice; potion_hp read deferred to P2.2.a).
        var amount = 100 * skillLevel;
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                _statusOps?.Heal(m, amount, 0, 0);
                ctx.Client?.BroadcastSkillNoDamage(src, m, SkillId, skillLevel);
            });
        }
    }
}
