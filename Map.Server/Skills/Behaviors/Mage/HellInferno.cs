using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_HELLINFERNO — Warlock Hell Inferno. Manual port of
/// <c>rathena-fork/src/map/skills/mage/hellinferno.cpp</c>.
///
/// <para>Two-element hit: Fire on initial impact, Dark on a 300 ms
/// follow-up. Ratio: <c>+(-100 + 400*lv)</c>, with +<c>200*lv</c> on
/// the Dark sub-hit. Splash dispatch + per-element ratio flag are
/// approximated — the second (Dark) attack is scheduled via the
/// skill-timer service.</para>
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

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Primary hit (Fire). Dark follow-up's +200*lv bonus is TODO — same hook can't see the flag.
        return baseRatio + (-100 + 400 * skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        // 300 ms Dark follow-up.
        _timers?.Schedule(src, target, 300, SkillId, skillLevel,
            (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv));
    }
}
