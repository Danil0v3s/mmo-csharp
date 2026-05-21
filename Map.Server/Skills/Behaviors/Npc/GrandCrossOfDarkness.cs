using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_GRANDDARKNESS — Magic hit (mob Grand Cross variant).</summary>
public sealed class GrandCrossOfDarkness : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public GrandCrossOfDarkness() : base(SkillIds.NPC_GRANDDARKNESS) { }
    public GrandCrossOfDarkness(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_GRANDDARKNESS) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
