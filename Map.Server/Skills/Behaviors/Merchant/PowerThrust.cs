using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_OVERTHRUST — Blacksmith Power Thrust (Over Thrust). Manual port
/// of <c>rathena-fork/src/map/skills/merchant/powerthrust.cpp</c>.
/// Party-wide ATK buff. Party splash + weapon-type gate TODO.
/// </summary>
public sealed class PowerThrust : SkillImpl
{
    public PowerThrust() : base(SkillIds.BS_OVERTHRUST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var selfFlag = src.Id == target.Id ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Overthrust, val1: skillLevel, val2: selfFlag, 0, 0, durationMs: 180_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
