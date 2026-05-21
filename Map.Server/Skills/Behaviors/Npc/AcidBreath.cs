using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ACIDBREATH — Ratio +100*(lv-1); 70% SC_POISON on hit.</summary>
public sealed class AcidBreath : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public AcidBreath() : base(SkillIds.NPC_ACIDBREATH) { }
    public AcidBreath(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_ACIDBREATH) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 70)
            ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
