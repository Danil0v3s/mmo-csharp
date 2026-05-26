using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_M_MACHINE — Meister Manufacture Machine. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/manufacturemachine.cpp</c>.
/// Opens the Meister crafting panel (rAthena
/// <c>clif_cooking_list(sd, 31, MT_M_MACHINE, 1, 7)</c>). Wire packet
/// reuses <c>ZC_MAKABLEITEMLIST</c> via
/// <see cref="ISkillClientService.BroadcastCookingList"/>; the
/// craftable-row lookup is sent empty until the produce-recipe
/// catalog is plumbed through <see cref="IProduceRecipeService"/>.
/// </summary>
public sealed class ManufactureMachine : SkillImpl
{
    public ManufactureMachine() : base(SkillIds.MT_M_MACHINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        // rAthena clif_cooking_list(sd, 31, MT_M_MACHINE, 1, 7) — produce-type 31
        // is the Mado-mechanic Manufacture panel. craftableItemIds left empty
        // until IProduceRecipeService can filter by per-recipe material check.
        ctx.Client?.BroadcastCookingList(sd, produceType: 31, craftableItemIds: Array.Empty<ushort>());
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
