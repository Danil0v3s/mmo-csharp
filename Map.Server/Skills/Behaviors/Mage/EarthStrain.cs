using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_EARTHSTRAIN — Warlock Earth Strain. Manual port of
/// <c>rathena-fork/src/map/skills/mage/earthstrain.cpp</c>.
///
/// <para>Wave skill that lays down (lv+4) wave tiles in the caster's
/// facing direction with 140 ms stagger between waves. Ratio:
/// <c>+(-100 + 1000 + 600*lv)</c>. Per-wave casting uses
/// <c>skill_addtimerskill</c>; the C# port schedules each wave on
/// <see cref="ISkillTimerService"/> and lands the attack at the
/// caster's position via the skill-attack service.</para>
///
/// <para>INFRA-DEFERRED: the precise XY positional cast chain
/// (each wave should land one cell further in the caster's facing
/// direction with the ground-position cast variant) needs a
/// position-targeted timer overload that doesn't exist on
/// <see cref="ISkillTimerService"/> today.</para>
/// </summary>
public sealed class EarthStrain : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly ISkillAttackService? _skillAttack;

    public EarthStrain() : base(SkillIds.WL_EARTHSTRAIN) { }

    public EarthStrain(
        ISkillTimerService? timers = null,
        ISkillAttackService? skillAttack = null) : base(SkillIds.WL_EARTHSTRAIN)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 1000 + 600*lv.
        return baseRatio + (-100 + 1000 + 600 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: emits (lv+4) waves stepping outward from the caster, 140 ms apart.
        // We schedule each wave on the timer service so it lands across ticks.
        var waves = skillLevel + 4;
        for (var w = 1; w <= waves; w++)
        {
            var delay = 140 * w;
            _timers?.Schedule(src, src, delay, SkillId, skillLevel,
                (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv));
        }
    }
}
