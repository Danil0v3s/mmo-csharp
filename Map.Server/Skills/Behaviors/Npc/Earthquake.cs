using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_EARTHQUAKE — Splash misc-type damage. Splash iteration TODO.</summary>
public sealed class Earthquake : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public Earthquake() : base(SkillIds.NPC_EARTHQUAKE) { }
    public Earthquake(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_EARTHQUAKE) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
}
