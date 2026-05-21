using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGDASH — Ranger Warg Dash. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargdash.cpp</c>.
/// Ratio +200. Toggle SC_WUGDASH on/off; warg-riding gate TODO.
/// </summary>
public sealed class WargDash : RecursiveDamageSplashSkillImpl
{
    public WargDash() : base(SkillIds.RA_WUGDASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.End(target, StatusType.Wugdash))
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Wugdash, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
