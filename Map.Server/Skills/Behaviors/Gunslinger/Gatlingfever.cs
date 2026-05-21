using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_GATLINGFEVER — Gunslinger Gatling Fever toggle. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/gatlingfever.cpp</c>.
/// Toggles SC_GATLINGFEVER on/off.
/// </summary>
public sealed class Gatlingfever : SkillImpl
{
    public Gatlingfever() : base(SkillIds.GS_GATLINGFEVER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Gatlingfever) != null)
            ctx.Sc.End(target, StatusType.Gatlingfever);
        else
            ctx.Sc?.Start(target, StatusType.Gatlingfever, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
