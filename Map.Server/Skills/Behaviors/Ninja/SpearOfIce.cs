using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_HYOUSENSOU — Spear Of Ice. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/spearofice.cpp</c>.
/// Renewal: <c>-30 + 2*lv</c> if SC_SUITON; +20 per water charm. Magic
/// single hit.
/// </summary>
public sealed class SpearOfIce : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SpearOfIce() : base(SkillIds.NJ_HYOUSENSOU) { }

    public SpearOfIce(ISkillAttackService? skillAttack = null) : base(SkillIds.NJ_HYOUSENSOU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio - 30;
        // We don't have ctx in this hook, so the SC_SUITON test is deferred — TODO.
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
