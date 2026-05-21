using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ICEBREATH2 — Magic hit variant.</summary>
public sealed class IceBreath2 : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public IceBreath2() : base(SkillIds.NPC_ICEBREATH2) { }
    public IceBreath2(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_ICEBREATH2) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
