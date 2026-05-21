using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_JUPITEL — Wizard Jupitel Thunder. Manual port of
/// <c>rathena-fork/src/map/skills/mage/jupitelthunder.cpp</c>.
///
/// <para>Wind-element multi-hit magic with delayed damage (the
/// rAthena comment: "Jupitel Thunder is delayed by 150ms, you can
/// cast another spell before the knockback"). Damage fires
/// 150 ms after cast resolution via the skill-timer scheduler.</para>
/// </summary>
public sealed class JupitelThunder : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public JupitelThunder() : base(SkillIds.WZ_JUPITEL) { }

    public JupitelThunder(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.WZ_JUPITEL)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_addtimerskill(src, tick + TIMERSKILL_INTERVAL, ...) — 150 ms delay.
        _timers?.Schedule(src, target, delayMs: 150, SkillId, skillLevel,
            (s, t, lv) =>
            {
                _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv);
            });
    }
}
