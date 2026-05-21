using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_FIRESTORM — Mirrors
/// <c>rathena-fork/src/map/skills/npc/firestorm.cpp</c>.
/// Magic splash (single-cast AoE, NOT a placed ground unit despite
/// the earlier stub). Ratio +200; 100% SC_BURNT on hit. Splash
/// iteration delegates to the magic damage pipeline through
/// <see cref="ISkillAttackService.SkillAttack"/>.
/// </summary>
public sealed class FireStorm : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public FireStorm() : base(SkillIds.NPC_FIRESTORM) { }
    public FireStorm(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_FIRESTORM) { _skillAttack = skillAttack; }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Burnt, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
}
