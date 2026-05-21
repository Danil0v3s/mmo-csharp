using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_FLING — Gunslinger Fling. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/fling.cpp</c>.
/// Lands a single hit + applies SC_FLING with val1 = consumed coins
/// (default 5 pending coin tracking).
/// </summary>
public sealed class Fling : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Fling() : base(SkillIds.GS_FLING) { }

    public Fling(ISkillAttackService? skillAttack = null) : base(SkillIds.GS_FLING)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Fling, val1: 5, 0, 0, 0, durationMs: 10_000, src);
}
