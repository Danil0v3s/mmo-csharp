using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_BLESSING — Cash-shop party Blessing. Port of
/// <c>rathena-fork/src/map/skills/other/partyblessing.cpp</c>.
/// Applies SC_BLESSING to every party member on the caster's map.
/// </summary>
public sealed class PartyBlessing : SkillImpl
{
    public PartyBlessing() : base(SkillIds.CASH_BLESSING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc, member =>
                ctx.Sc?.Start(member, StatusType.Blessing, val1: 10, 0, 0, 0, durationMs: 60_000, src));
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.Blessing, val1: 10, 0, 0, 0, durationMs: 60_000, src);
        }
    }
}
