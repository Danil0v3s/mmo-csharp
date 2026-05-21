using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_RISING_SUN — Manual port. Hit + applies SC_RISING_SUN to caster.</summary>
public sealed class RisingSun : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public RisingSun() : base(SkillIds.SKE_RISING_SUN) { }
    public RisingSun(ISkillAttackService? skillAttack = null) : base(SkillIds.SKE_RISING_SUN) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
