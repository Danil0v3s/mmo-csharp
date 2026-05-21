using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_AXE_STOMP — Meister Axe Stomp. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/axestomp.cpp</c>.
/// Ratio: <c>+(-100 + 450 + 1150*lv) + 5*POW</c>. Applies SC_AXE_STOMP
/// then fires the splash damage.
/// </summary>
public sealed class AxeStomp : RecursiveDamageSplashSkillImpl
{
    public AxeStomp() : base(SkillIds.MT_AXE_STOMP) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 + 1150 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.AxeStomp, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
