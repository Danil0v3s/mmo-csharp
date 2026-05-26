using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_PARTYFLEE — Cash-shop party Flee buff. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyflee.cpp</c>.
/// Applies SC_PARTYFLEE to every party member on the caster's map.
/// </summary>
public sealed class PartyFlee : StatusSkillImpl
{
    public PartyFlee() : base(SkillIds.ALL_PARTYFLEE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc, member =>
                ctx.Sc?.Start(member, StatusType.Partyflee, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src));
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.Partyflee, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        }
    }
}
