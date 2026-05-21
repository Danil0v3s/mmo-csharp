using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_BACKSTAP — Back Stab. Manual port of
/// <c>rathena-fork/src/map/skills/thief/backstab.cpp</c>.
/// Renewal: 2 hits with dagger (div_ = 2). Ratio <c>+(200 + 40*lv)</c>,
/// halved when wielding a bow (battle_config.backstab_bow_penalty).
/// Behind-target slide is TODO.
/// </summary>
public sealed class BackStab : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public BackStab() : base(SkillIds.RG_BACKSTAP) { }

    public BackStab(ISkillAttackService? skillAttack = null) : base(SkillIds.RG_BACKSTAP)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200 + 40 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
