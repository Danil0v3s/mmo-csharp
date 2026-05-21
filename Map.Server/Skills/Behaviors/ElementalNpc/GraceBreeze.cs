using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_GRACE_BREEZE — Elemental Grace Breeze. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/gracebreeze.cpp</c>.
/// Toggles SC_GRACE_BREEZE on target + SC_GRACE_BREEZE_OPTION on the
/// elemental.
/// </summary>
public sealed class GraceBreeze : SkillImpl
{
    public GraceBreeze() : base(SkillIds.EM_EL_GRACE_BREEZE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.GraceBreeze) != null
            || ctx.Sc?.Get(src, StatusType.GraceBreezeOption) != null)
        {
            ctx.Sc.End(target, StatusType.GraceBreeze);
            ctx.Sc.End(src, StatusType.GraceBreezeOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.GraceBreezeOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.GraceBreeze, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
