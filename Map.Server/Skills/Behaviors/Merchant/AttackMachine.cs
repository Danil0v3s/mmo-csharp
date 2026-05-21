using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_A_MACHINE — Meister Attack Machine. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/attackmachine.cpp</c>.
/// Ratio: <c>+(-100 + 200*lv) + 5*POW</c>. Marks the target with
/// SC_A_MACHINE which fires repeat splash hits per tick.
/// </summary>
public sealed class AttackMachine : RecursiveDamageSplashSkillImpl
{
    public AttackMachine() : base(SkillIds.MT_A_MACHINE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.AMachine, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
