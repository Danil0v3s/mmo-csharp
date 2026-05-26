using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_PHARMACY — Alchemist Prepare Potion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/preparepotion.cpp</c>.
/// Opens the produce-mix list (rAthena
/// <c>clif_skill_produce_mix_list(sd, AM_PHARMACY, 22)</c>). The
/// produce-mix wire packet shares <c>ZC_MAKABLEITEMLIST</c> with the
/// cooking list, routed via
/// <see cref="ISkillClientService.BroadcastProduceMixList"/>;
/// craftable rows left empty until <see cref="IProduceRecipeService"/>
/// can resolve per-skill recipes.
/// </summary>
public sealed class PreparePotion : SkillImpl
{
    public PreparePotion() : base(SkillIds.AM_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        ctx.Client?.BroadcastProduceMixList(sd, produceType: 22, craftableItemIds: Array.Empty<ushort>());
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
