using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKSTRIKE — Magic shadow soul strike.</summary>
public sealed class SoulStrikeOfDarkness : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public SoulStrikeOfDarkness() : base(SkillIds.NPC_DARKSTRIKE) { }
    public SoulStrikeOfDarkness(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_DARKSTRIKE) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 40 * skillLevel;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
