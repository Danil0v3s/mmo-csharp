using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_PARTYFLEE — Cash-shop party Flee buff. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyflee.cpp</c>.
/// Applies SC_PARTYFLEE; party splash is TODO.
/// </summary>
public sealed class PartyFlee : StatusSkillImpl
{
    public PartyFlee() : base(SkillIds.ALL_PARTYFLEE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Partyflee, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
