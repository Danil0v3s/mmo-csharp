using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_TWINKLING_GALAXY — Manual port. Weapon hit.</summary>
public sealed class TwinklingGalaxy : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public TwinklingGalaxy() : base(SkillIds.SKE_TWINKLING_GALAXY) { }
    public TwinklingGalaxy(ISkillAttackService? skillAttack = null) : base(SkillIds.SKE_TWINKLING_GALAXY) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
