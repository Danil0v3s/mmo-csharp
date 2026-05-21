using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_BLACK_TORTOISE — Ratio +(-100 + 2150 + 1600*lv). Magic hit.</summary>
public sealed class TalismanOfBlackTortoise : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public TalismanOfBlackTortoise() : base(SkillIds.SOA_TALISMAN_OF_BLACK_TORTOISE) { }
    public TalismanOfBlackTortoise(ISkillAttackService? skillAttack = null) : base(SkillIds.SOA_TALISMAN_OF_BLACK_TORTOISE) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 2150 + 1600 * skillLevel);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
