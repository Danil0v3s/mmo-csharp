using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKPIERCING — Magic hit.</summary>
public sealed class DarkPiercing : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public DarkPiercing() : base(SkillIds.NPC_DARKPIERCING) { }
    public DarkPiercing(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_DARKPIERCING) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
