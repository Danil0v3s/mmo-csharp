using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_ADRENALINE — Blacksmith Adrenaline Rush. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/adrenalinerush.cpp</c>.
/// Party-wide buff (axe/mace only). Weapon-type gate + party splash
/// TODO. val2 = 1 if self-cast (per rAthena).
/// </summary>
public sealed class AdrenalineRush : SkillImpl
{
    public AdrenalineRush() : base(SkillIds.BS_ADRENALINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var selfFlag = src.Id == target.Id ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Adrenaline, val1: skillLevel, val2: selfFlag, 0, 0, durationMs: 150_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
