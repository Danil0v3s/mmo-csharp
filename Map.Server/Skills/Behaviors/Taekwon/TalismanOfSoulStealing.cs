using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_SOUL_STEALING — Ratio +(-100 + 500 + 1250*lv). Magic hit.</summary>
public sealed class TalismanOfSoulStealing : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public TalismanOfSoulStealing() : base(SkillIds.SOA_TALISMAN_OF_SOUL_STEALING) { }
    public TalismanOfSoulStealing(ISkillAttackService? skillAttack = null) : base(SkillIds.SOA_TALISMAN_OF_SOUL_STEALING) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 + 1250 * skillLevel);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
