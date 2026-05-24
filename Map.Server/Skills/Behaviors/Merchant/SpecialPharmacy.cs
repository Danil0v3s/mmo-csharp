using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_S_PHARMACY — Genetic Special Pharmacy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/specialpharmacy.cpp</c>.
/// Stashes skill_id_old / skill_lv_old and opens cooking list type 29
/// (qty=1, page=6). The cooking dialog UI is not yet wired, so we
/// broadcast the no-damage animation and TODO the cook list packet.
/// </summary>
public sealed class SpecialPharmacy : SkillImpl
{
    public SpecialPharmacy() : base(SkillIds.GN_S_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena clif_cooking_list(sd, 29, GN_S_PHARMACY, 1, 6) — open the
        // Genetic Special Pharmacy panel. The craftable-list lookup (per-recipe
        // material check) isn't ported yet, so the list is sent empty; the
        // dialog still surfaces.
        if (src is PlayerEntity sd)
            ctx.Client?.BroadcastCookingList(sd, produceType: 29, craftableItemIds: Array.Empty<ushort>());
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
