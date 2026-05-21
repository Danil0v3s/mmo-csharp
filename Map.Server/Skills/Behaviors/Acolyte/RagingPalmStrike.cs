using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_PALMSTRIKE — Champion Raging Palm Strike. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ragingpalmstrike.cpp</c>.
///
/// <para>Delayed weapon hit — rAthena comment: "Palm Strike takes
/// effect 1sec after casting." The hit fires 1000 ms + caster
/// amotion after the cast resolves; the immediate frame shows an
/// absorbed-damage placeholder.</para>
///
/// <para>Renewal ratio: <c>100 + 100 * lv + STR</c>.</para>
/// </summary>
public sealed class RagingPalmStrike : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public RagingPalmStrike() : base(SkillIds.CH_PALMSTRIKE) { }

    public RagingPalmStrike(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.CH_PALMSTRIKE)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Schedule the delayed weapon hit at tick + 1000 + amotion.
        var delay = 1000 + src.Stats.Amotion;
        _timers?.Schedule(src, target, delay, SkillId, skillLevel,
            (s, t, lv) =>
            {
                _skillAttack?.SkillAttack(BattleAttackType.Weapon, s, s, t, SkillId, lv);
            });
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 100 + 100 * skillLevel + src.Stats.Str;
    }
}
