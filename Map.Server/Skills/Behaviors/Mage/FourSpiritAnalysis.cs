using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_ANALYSIS — Sorcerer Four-Spirit Analysis
/// (skill.cpp:SO_EL_ANALYSIS arm). Opens the item-selection chooser
/// (<c>clif_skill_itemlistwindow</c>) so the caster picks the source
/// ore. The pick routes to <see cref="ISkillProductionService.ElementalAnalysis"/>.
/// </summary>
public sealed class FourSpiritAnalysis : SkillImpl
{
    public FourSpiritAnalysis() : base(SkillIds.SO_EL_ANALYSIS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(pc);
        var inv = session?.Inventory;
        if (inv == null) return;
        var sources = new List<ushort>();
        foreach (var row in inv)
        {
            if (row.Amount > 0 && row.NameId is > 0 and <= ushort.MaxValue)
                sources.Add((ushort)row.NameId);
        }
        ctx.Client?.BroadcastItemListWindow(pc, sources);
    }
}
