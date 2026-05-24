using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_BIONIC_PHARMACY — Biolo Bionic Pharmacy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/bionicpharmacy.cpp</c>.
/// Opens the cooking-list crafting panel. clif_cooking_list not wired —
/// broadcast only.
/// </summary>
public sealed class BionicPharmacy : SkillImpl
{
    public BionicPharmacy() : base(SkillIds.BO_BIONIC_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena clif_cooking_list(sd, 32, BO_BIONIC_PHARMACY, 1, 8) — opens
        // the Biolo crafting panel. The recipe-material check isn't ported
        // yet, so the craftable list goes out empty for now.
        ctx.Client?.BroadcastCookingList(sd, produceType: 32, craftableItemIds: Array.Empty<ushort>());
    }
}
