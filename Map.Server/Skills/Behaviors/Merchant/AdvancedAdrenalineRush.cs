using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_ADRENALINE2 — Blacksmith Advanced Adrenaline Rush. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/advancedadrenalinerush.cpp</c>.
/// Same shape as <see cref="AdrenalineRush"/>; SC_ADRENALINE2 (works
/// on more weapon types).
/// </summary>
public sealed class AdvancedAdrenalineRush : SkillImpl
{
    public AdvancedAdrenalineRush() : base(SkillIds.BS_ADRENALINE2) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var selfFlag = src.Id == target.Id ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Adrenaline2, val1: skillLevel, val2: selfFlag, 0, 0, durationMs: 150_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
