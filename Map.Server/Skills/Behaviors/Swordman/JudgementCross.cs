using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_JUDGEMENT_CROSS — Imperial Guard Judgement Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/judgementcross.cpp</c>.
/// Ratio <c>+(-100 + 1950*lv) + 10*SPL</c>; +150*lv vs Plant/Insect.
/// Imperial-guard skilltree bonus is TODO.
/// </summary>
public sealed class JudgementCross : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public JudgementCross() : base(SkillIds.IG_JUDGEMENT_CROSS) { }

    public JudgementCross(ISkillAttackService? skillAttack = null) : base(SkillIds.IG_JUDGEMENT_CROSS)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 1950 * skillLevel) + 10 * src.Stats.Spl;
        if (target.Stats.Race == BattleRace.Plant || target.Stats.Race == BattleRace.Insect)
            ratio += 150 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
