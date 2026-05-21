using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_DRAINLIFE — Warlock Drain Life. Manual port of
/// <c>rathena-fork/src/map/skills/mage/drainlife.cpp</c>.
///
/// <para>Single-target Dark magic. Ratio: <c>+(-100 + 200*lv) + INT</c>.
/// Caster heals <c>(5 + 5*lv) %</c> of damage dealt at a
/// <c>70 + 5*lv %</c> rate. Damage value from the skill_attack return is
/// not directly observable from this hook — we approximate the heal from
/// our locally-computed nominal damage; tighten if/when the skill engine
/// surfaces post-mitigation damage to the behavior.</para>
/// </summary>
public sealed class DrainLife : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public DrainLife() : base(SkillIds.WL_DRAINLIFE) => _rng = Random.Shared;

    public DrainLife(
        ISkillAttackService? skillAttack = null,
        IStatusOpsService? statusOps = null,
        Random? rng = null) : base(SkillIds.WL_DRAINLIFE)
    {
        _skillAttack = skillAttack;
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 200*lv + INT.
        return baseRatio + (-100 + 200 * skillLevel) + src.Stats.IntStat;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);

        // rAthena heals (5+5*lv)% of damage dealt at a (70+5*lv)% chance.
        var rate = 70 + 5 * skillLevel;
        if (_rng.Next(100) >= rate) return;
        // Damage value isn't surfaced here; use a nominal heal until the skill engine returns the hit value.
        var nominalHeal = Math.Max(1, (5 + 5 * skillLevel) * 10 * skillLevel);
        _statusOps?.Heal(src, nominalHeal, 0, 0);
    }
}
