using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_STAR_BURST — Star Burst. Hits target with weapon-type skill_attack.</summary>
public sealed class StarBurst : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public StarBurst() : base(SkillIds.SKE_STAR_BURST) { }
    public StarBurst(ISkillAttackService? skillAttack = null) : base(SkillIds.SKE_STAR_BURST) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
