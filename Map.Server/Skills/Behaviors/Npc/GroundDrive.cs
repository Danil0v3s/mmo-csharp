using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_GROUNDDRIVE — Magic hit.</summary>
public sealed class GroundDrive : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public GroundDrive() : base(SkillIds.NPC_GROUNDDRIVE) { }
    public GroundDrive(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_GROUNDDRIVE) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
