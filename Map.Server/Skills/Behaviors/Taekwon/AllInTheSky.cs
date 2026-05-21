using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_ALL_IN_THE_SKY — Manual port. Ratio +(-100 + 250 + 1200*lv) + 5*pow.</summary>
public sealed class AllInTheSky : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public AllInTheSky() : base(SkillIds.SKE_ALL_IN_THE_SKY) { }
    public AllInTheSky(ISkillAttackService? skillAttack = null) : base(SkillIds.SKE_ALL_IN_THE_SKY) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 + 1200 * skillLevel) + 5 * src.Stats.Pow;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
