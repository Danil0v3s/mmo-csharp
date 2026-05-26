using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_ASSUMPTIO — Cash-shop party Assumptio. Port of
/// <c>rathena-fork/src/map/skills/other/partyassumptio.cpp</c>.
/// Applies SC_ASSUMPTIO to every party member on the caster's map.
/// </summary>
public sealed class PartyAssumptio : SkillImpl
{
    public PartyAssumptio() : base(SkillIds.CASH_ASSUMPTIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc, member =>
                ctx.Sc?.Start(member, StatusType.Assumptio, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src));
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.Assumptio, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        }
    }
}
