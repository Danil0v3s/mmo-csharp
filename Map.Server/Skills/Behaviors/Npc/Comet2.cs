using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_COMET — Magic hit + 100% SC_BURNING on hit.</summary>
public sealed class Comet2 : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public Comet2() : base(SkillIds.NPC_COMET) { }
    public Comet2(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_COMET) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Burning, val1: skillLevel, val2: 1000, val3: (int)src.Id.Value, 0, durationMs: 10_000, src);
}
