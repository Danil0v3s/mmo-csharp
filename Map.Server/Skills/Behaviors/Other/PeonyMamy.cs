using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ECL_PEONYMAMY — Peony Mamy frost cleanse. Manual port of
/// <c>rathena-fork/src/map/skills/other/peonymamy.cpp</c>.
/// Cleanses SC_FREEZE, SC_FREEZING, SC_CRYSTALIZE.
/// </summary>
public sealed class PeonyMamy : SkillImpl
{
    public PeonyMamy() : base(SkillIds.ECL_PEONYMAMY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Freeze);
        ctx.Sc?.End(target, StatusType.Freezing);
        ctx.Sc?.End(target, StatusType.Crystalize);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
