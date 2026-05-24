using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_ANALYSIS — Sorcerer Four-Spirit Analysis. Opens the
/// item-selection list on the caster's client. The chooser packet
/// (ZC_ITEMLISTWIN_OPEN) is TODO.
/// </summary>
public sealed class FourSpiritAnalysis : SkillImpl
{
    public FourSpiritAnalysis() : base(SkillIds.SO_EL_ANALYSIS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: ZC_ITEMLISTWIN_OPEN item-selection packet isn't ported yet —
        // the chooser UI is opened in rAthena via clif_skill_itemlistwindow.
    }
}
