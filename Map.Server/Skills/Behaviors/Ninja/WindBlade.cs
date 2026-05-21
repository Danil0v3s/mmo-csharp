using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_HUUJIN — Wind Blade. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/windblade.cpp</c>.
/// Renewal: +50 ratio (wind-charm bonus TODO). Magic single hit.
/// </summary>
public sealed class WindBlade : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public WindBlade() : base(SkillIds.NJ_HUUJIN) { }

    public WindBlade(ISkillAttackService? skillAttack = null) : base(SkillIds.NJ_HUUJIN)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
