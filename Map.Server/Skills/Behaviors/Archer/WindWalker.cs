using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_WINDWALK — Sniper Wind Walker. Manual port of
/// <c>rathena-fork/src/map/skills/archer/windwalker.cpp</c>.
/// Party-wide ASPD/MOVE buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class WindWalker : SkillImpl
{
    public WindWalker() : base(SkillIds.SN_WINDWALK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Windwalk, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
