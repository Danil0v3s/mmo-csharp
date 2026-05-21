using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_THROWSTONE — Stone Fling. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stonefling.cpp</c>.
/// Single-target hit; 3% stun OR 3% blind on player, 5% stun on mob.
/// </summary>
public sealed class StoneFling : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public StoneFling() : base(SkillIds.TF_THROWSTONE) { }

    public StoneFling(ISkillAttackService? skillAttack = null) : base(SkillIds.TF_THROWSTONE)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity)
        {
            if (System.Random.Shared.Next(100) < 3)
                ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
            else if (System.Random.Shared.Next(100) < 3)
                ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
        else
        {
            if (System.Random.Shared.Next(100) < 5)
                ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        }
    }
}
