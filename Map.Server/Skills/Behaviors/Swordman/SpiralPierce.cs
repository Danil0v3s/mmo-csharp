using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LK_SPIRALPIERCE — Lord Knight Spiral Pierce. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/spiralpierce.cpp</c>.
/// Renewal ratio <c>+50 + 50*lv</c>; ChargingPierceCount × 2 multiplier
/// is TODO. On hit, 100% SC_ANKLE on non-status-immune targets.
/// </summary>
public sealed class SpiralPierce : WeaponSkillImpl
{
    public SpiralPierce() : base(SkillIds.LK_SPIRALPIERCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 50 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity && (target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        ctx.Sc?.Start(target, StatusType.Ankle, val1: 0, 0, 0, 0, durationMs: 10_000, src);
    }
}
