using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_INCAGI — Cash-shop party Increase Agi. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyincreaseagi.cpp</c>.
/// Applies SC_INCREASEAGI; party splash is TODO.
/// </summary>
public sealed class PartyIncreaseAgi : SkillImpl
{
    public PartyIncreaseAgi() : base(SkillIds.CASH_INCAGI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.IncreaseAgi, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
