using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_HELLBURNING — Magic hit; +900 ratio.</summary>
public sealed class HellBurning : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public HellBurning() : base(SkillIds.NPC_HELLBURNING) { }
    public HellBurning(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_HELLBURNING) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
