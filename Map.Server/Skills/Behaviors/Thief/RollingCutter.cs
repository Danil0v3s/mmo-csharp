using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_ROLLINGCUTTER — Rolling Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/thief/rollingcutter.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 50 + 80*lv)</c>. Each cast
/// bumps SC_ROLLINGCUTTER on the cast target (cap 10) so chained
/// casts ramp the spin count up. The SC.Val1 is what
/// SHC_IMPACT_CRATER and GC_CROSSRIPPERSLASHER read for their
/// per-spin damage scaling.
/// </summary>
public sealed class RollingCutter : RecursiveDamageSplashSkillImpl
{
    /// <summary>rAthena <c>if (count > 10) count = 10</c> — spin cap.</summary>
    private const int ROLLING_CUTTER_MAX = 10;

    public RollingCutter() : base(SkillIds.GC_ROLLINGCUTTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 + 80 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: short count = 1; if SC_ROLLINGCUTTER active, count +=
        //   val1 (cap 10); status_change_end + sc_start with new count.
        var existing = ctx.Sc?.Get(target, StatusType.Rollingcutter);
        var count = 1;
        if (existing != null)
        {
            count = System.Math.Min(ROLLING_CUTTER_MAX, count + existing.Val1);
            ctx.Sc!.End(target, StatusType.Rollingcutter);
        }
        ctx.Sc?.Start(target, StatusType.Rollingcutter, val1: count, 0, 0, 0,
            durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);

        // Splash damage to enemies in range — delegated to the
        // recursive splash base via CastendDamageId.
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
