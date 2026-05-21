using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ECL_SEQUOIADUST — Sequoia Dust cleanse. Manual port of
/// <c>rathena-fork/src/map/skills/other/sequoiadust.cpp</c>.
/// Cleanses Stone / Poison / Curse / Blind / Orcish / DecreaseAgi.
/// </summary>
public sealed class SequoiaDust : SkillImpl
{
    public SequoiaDust() : base(SkillIds.ECL_SEQUOIADUST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Stone);
        ctx.Sc?.End(target, StatusType.Poison);
        ctx.Sc?.End(target, StatusType.Curse);
        ctx.Sc?.End(target, StatusType.Blind);
        ctx.Sc?.End(target, StatusType.Orcish);
        ctx.Sc?.End(target, StatusType.DecreaseAgi);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
