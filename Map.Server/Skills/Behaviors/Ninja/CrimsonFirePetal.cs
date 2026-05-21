using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KOUENKA — Crimson Fire Petal. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/crimsonfirepetal.cpp</c>.
/// -10 base ratio + 10*charm when CHARM_TYPE_FIRE. Magic single hit.
/// </summary>
public sealed class CrimsonFirePetal : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public CrimsonFirePetal() : base(SkillIds.NJ_KOUENKA) { }

    public CrimsonFirePetal(ISkillAttackService? skillAttack = null) : base(SkillIds.NJ_KOUENKA)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio - 10;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
