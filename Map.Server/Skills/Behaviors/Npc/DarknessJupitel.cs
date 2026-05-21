using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKTHUNDER — Magic single hit.</summary>
public sealed class DarknessJupitel : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public DarknessJupitel() : base(SkillIds.NPC_DARKTHUNDER) { }
    public DarknessJupitel(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_DARKTHUNDER) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
