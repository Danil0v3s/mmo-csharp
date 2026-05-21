using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKBREATH — Magic single hit.</summary>
public sealed class DarkBreath : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public DarkBreath() : base(SkillIds.NPC_DARKBREATH) { }
    public DarkBreath(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_DARKBREATH) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
