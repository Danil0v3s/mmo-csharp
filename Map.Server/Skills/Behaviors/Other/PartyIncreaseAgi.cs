using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_INCAGI — Cash-shop party Increase Agi. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyincreaseagi.cpp</c>.
/// Applies SC_INCREASEAGI to every party member on the caster's map.
/// </summary>
public sealed class PartyIncreaseAgi : SkillImpl
{
    public PartyIncreaseAgi() : base(SkillIds.CASH_INCAGI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc, member =>
                ctx.Sc?.Start(member, StatusType.IncreaseAgi, val1: 10, 0, 0, 0, durationMs: 60_000, src));
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.IncreaseAgi, val1: 10, 0, 0, 0, durationMs: 60_000, src);
        }
    }
}
