using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_S_PHARMACY — Genetic Special Pharmacy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/specialpharmacy.cpp</c>.
/// Opens the Genetic Special Pharmacy panel via
/// <see cref="ISkillClientService.BroadcastCookingList"/>
/// (rAthena <c>clif_cooking_list(sd, 29, GN_S_PHARMACY, 1, 6)</c>).
/// The craftable-row catalog is sent empty until per-recipe material
/// checks land via <see cref="IProduceRecipeService"/>; the dialog
/// still surfaces on the client.
///
/// <para>rAthena also stashes <c>sd-&gt;skill_id_old</c> /
/// <c>skill_lv_old</c> for the subsequent produce_mix RPC. That state
/// belongs on the session, not the entity — 🚩 INFRA-DEFERRED until
/// <see cref="MapSessionData"/> carries the "last produce skill"
/// pair.</para>
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
