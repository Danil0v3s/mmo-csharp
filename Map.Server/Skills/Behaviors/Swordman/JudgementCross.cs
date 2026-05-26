using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_JUDGEMENT_CROSS — Imperial Guard Judgement Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/judgementcross.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1950*lv) + 10*SPL</c>; <c>+150*lv</c> vs
/// Plant / Insect.</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena applies a final
/// <c>ratio + ratio * pc_checkskill_imperial_guard(sd, 3) / 100</c>
/// scale where <c>pc_checkskill_imperial_guard(sd, 3)</c> sums the
/// caster's IG_GRAND_JUDGEMENT + IG_JUDGEMENT_CROSS + IG_OVERSLASH
/// + IG_CROSS_RAIN + IG_HOLY_SHIELD levels. Wiring this needs a
/// per-class composite checkskill helper on
/// <see cref="IPlayerSkillService"/> which we haven't ported yet.</para>
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
