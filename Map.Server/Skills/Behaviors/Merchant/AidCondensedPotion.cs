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
        // TODO: read potion_hp/potion_sp from inventory script + party splash.
        _statusOps?.Heal(target, 100 * skillLevel, 0, 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: party_foreachsamemap splash with potion-script heal.
    }
}
