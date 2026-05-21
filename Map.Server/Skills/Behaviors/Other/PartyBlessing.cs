using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_BLESSING — Cash-shop party Blessing. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyblessing.cpp</c>.
/// Applies SC_BLESSING; party splash is TODO.
/// </summary>
public sealed class PartyBlessing : SkillImpl
{
    public PartyBlessing() : base(SkillIds.CASH_BLESSING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Blessing, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
