using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_KNUCKLEARROW — Sura Knuckle Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/knucklearrow.cpp</c>.
///
/// <para>Cast resolution: slide the caster adjacent to the target,
/// then schedule a 300 ms delayed weapon hit. The deferred hit
/// fires through <see cref="ISkillAttackService"/> with BF_WEAPON
/// damage type — same shape as rAthena's
/// <c>skill_addtimerskill(... BF_WEAPON ...)</c>.</para>
///
/// <para>Ratio: first hit <c>400 + 100*lv</c> (regular) or
/// <c>400 + 200*lv</c> vs boss class. The post-slide second hit
/// (rAthena miscflag&amp;4) is shaped differently
/// (<c>-100 + 150*lv + 5*targetLv</c> + target-weight bonus); the
/// C# port falls back to the first-hit ratio since
/// CalculateSkillRatio doesn't carry the miscflag bit.</para>
/// </summary>
public sealed class KnuckleArrow : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    private readonly IUnitOpsService? _unitOps;

    public KnuckleArrow() : base(SkillIds.SR_KNUCKLEARROW) { }

    public KnuckleArrow(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null,
        IUnitOpsService? unitOps = null) : base(SkillIds.SR_KNUCKLEARROW)
    {
        _timers = timers;
        _skillAttack = skillAttack;
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        if (target is MobEntity m && (m.Stats.Mode & Map.Server.Status.MobMode.Mvp) != 0)
            return baseRatio + 400 + 200 * skillLevel;
        return baseRatio + 400 + 100 * skillLevel;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Slide caster to target's cell.
        _unitOps?.MovePos(src, target.X, target.Y, easy: 1, checkColl: true);

        // Schedule the 300-ms deferred weapon hit.
        _timers?.Schedule(src, target, delayMs: 300, SkillId, skillLevel,
            (s, t, lv) =>
            {
                _skillAttack?.SkillAttack(BattleAttackType.Weapon, s, s, t, SkillId, lv);
            });
    }
}
