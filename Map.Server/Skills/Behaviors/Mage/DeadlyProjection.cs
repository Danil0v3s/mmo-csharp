using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_DEADLY_PROJECTION — Arch Mage Deadly Projection. Applies
/// SC_DEADLY_DEFEASANCE on cast, then magic damage.
/// Ratio: +(-100 + 2800*lv + 5*SPL).
/// </summary>
public sealed class DeadlyProjection : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    public DeadlyProjection() : base(SkillIds.AG_DEADLY_PROJECTION) { }
    public DeadlyProjection(Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.AG_DEADLY_PROJECTION) => _skillAttack = skillAttack;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 2800 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.DeadlyDefeasance,
            val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
