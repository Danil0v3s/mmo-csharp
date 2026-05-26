using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_HELLINFERNO — Warlock Hell Inferno. Manual port of
/// <c>rathena-fork/src/map/skills/mage/hellinferno.cpp</c>.
///
/// <para>Two-element hit: Fire on initial impact, Dark on a 300 ms
/// follow-up. Ratio: <c>+(-100 + 400*lv)</c>, with +<c>200*lv</c> on
/// the Dark sub-hit (driven by <c>miscflag &amp; 2</c>, set when
/// scheduling the Dark timer). The second (Dark) attack is scheduled
/// via the skill-timer service.</para>
/// </summary>
public sealed class HellInferno : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillTimerService? _timers;

    public HellInferno() : base(SkillIds.WL_HELLINFERNO) { }

    public HellInferno(
        ISkillAttackService? skillAttack = null,
        ISkillTimerService? timers = null) : base(SkillIds.WL_HELLINFERNO)
    {
        _skillAttack = skillAttack;
        _timers = timers;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        // rAthena: skillratio += -100 + 400*lv; +200*lv when miscflag & 2 (ELE_DARK follow-up).
        var ratio = baseRatio + (-100 + 400 * skillLevel);
        if ((miscflag & 2) != 0)
            ratio += 200 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        // 300 ms Dark follow-up.
        _timers?.Schedule(src, target, 300, SkillId, skillLevel,
            (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv));
    }
}
