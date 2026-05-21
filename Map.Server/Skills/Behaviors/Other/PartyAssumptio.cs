using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// CASH_ASSUMPTIO — Cash-shop party Assumptio. Manual port of
/// <c>rathena-fork/src/map/skills/other/partyassumptio.cpp</c>.
/// Applies SC_ASSUMPTIO; party splash is TODO.
/// </summary>
public sealed class PartyAssumptio : SkillImpl
{
    public PartyAssumptio() : base(SkillIds.CASH_ASSUMPTIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Assumptio, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
