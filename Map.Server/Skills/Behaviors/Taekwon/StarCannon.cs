using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_STAR_CANNON — Star Cannon. Hits target with weapon-type skill_attack.</summary>
public sealed class StarCannon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public StarCannon() : base(SkillIds.SKE_STAR_CANNON) { }
    public StarCannon(ISkillAttackService? skillAttack = null) : base(SkillIds.SKE_STAR_CANNON) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
