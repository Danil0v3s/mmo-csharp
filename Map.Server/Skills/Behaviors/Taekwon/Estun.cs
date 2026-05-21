using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SL_STUN — Estun. Manual port. Ratio +5*lv; magic hit.</summary>
public sealed class Estun : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public Estun() : base(SkillIds.SL_STUN) { }
    public Estun(ISkillAttackService? skillAttack = null) : base(SkillIds.SL_STUN) { _skillAttack = skillAttack; }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 5 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
