using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_WATERBALL — Wizard Water Ball. Deploys waterball cells at the
/// caster's position and schedules the per-ball delivery via the
/// skill-timer scheduler. Ratio: <c>+30 * lv</c> per ball.
/// </summary>
public sealed class WaterBall : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public WaterBall() : base(SkillIds.WZ_WATERBALL) { }

    public WaterBall(
        ISkillUnitService? units = null,
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.WZ_WATERBALL)
    {
        _units = units;
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, src.X, src.Y);
        _timers?.Schedule(src, target, delayMs: 0, SkillId, skillLevel,
            (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;
}
