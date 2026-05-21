using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_ZENYNAGE — Throw Zeny. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/throwzeny.cpp</c>.
/// Single-target misc-type skill_attack. Zeny consumption is handled
/// in the cast/requirements layer.
/// </summary>
public sealed class ThrowZeny : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ThrowZeny() : base(SkillIds.NJ_ZENYNAGE) { }

    public ThrowZeny(ISkillAttackService? skillAttack = null) : base(SkillIds.NJ_ZENYNAGE)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
}
