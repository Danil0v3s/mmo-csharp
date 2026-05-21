using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_BLUE_DRAGON — Ratio +(-100 + 850 + 2250*lv). Magic hit.</summary>
public sealed class TalismanOfBlueDragon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public TalismanOfBlueDragon() : base(SkillIds.SOA_TALISMAN_OF_BLUE_DRAGON) { }
    public TalismanOfBlueDragon(ISkillAttackService? skillAttack = null) : base(SkillIds.SOA_TALISMAN_OF_BLUE_DRAGON) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 850 + 2250 * skillLevel);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
