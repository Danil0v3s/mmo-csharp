using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_IDENTIFY — Merchant Item Appraisal. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/itemappraisal.cpp</c>.
/// Opens the identify-item picker. UI packet TODO.
/// </summary>
public sealed class ItemAppraisal : SkillImpl
{
    public ItemAppraisal() : base(SkillIds.MC_IDENTIFY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: clif_item_identify_list emits ZC_ACK_ITEMIDENTIFY — the
        // identify-UI packet isn't ported and ISkillClientService doesn't expose it.
    }
}
