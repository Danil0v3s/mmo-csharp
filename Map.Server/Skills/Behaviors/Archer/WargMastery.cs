using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGMASTERY — Ranger Warg Mastery. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargmastery.cpp</c>.
/// Toggles OPTION_WUG (option service deferred).
/// </summary>
public sealed class WargMastery : SkillImpl
{
    public WargMastery() : base(SkillIds.RA_WUGMASTERY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: toggle OPTION_WUG via IPlayerOptionService — needs player-option service plumbed into ctx.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
