using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_IDENTIFY — Merchant Item Appraisal (skill.cpp:MC_IDENTIFY arm).
/// Emits <c>clif_item_identify_list</c> with every unidentified row in
/// the caster's bag. The pick routes to
/// <see cref="ISkillProductionService.Identify"/>.
/// </summary>
public sealed class ItemAppraisal : SkillImpl
{
    public ItemAppraisal() : base(SkillIds.MC_IDENTIFY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(pc);
        var inv = session?.Inventory;
        if (inv == null) return;
        var unidentified = new List<ushort>();
        foreach (var row in inv)
        {
            if (!row.Identified && row.NameId is > 0 and <= ushort.MaxValue)
                unidentified.Add((ushort)row.NameId);
        }
        ctx.Client?.BroadcastItemIdentifyList(pc, unidentified);
    }
}
