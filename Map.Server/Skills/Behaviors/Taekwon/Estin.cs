using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SL_STIN — Estin. Manual port. Ratio +10*lv vs small targets, -99 otherwise (size penalty TODO).</summary>
public sealed class Estin : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public Estin() : base(SkillIds.SL_STIN) { }
    public Estin(ISkillAttackService? skillAttack = null) : base(SkillIds.SL_STIN) { _skillAttack = skillAttack; }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
