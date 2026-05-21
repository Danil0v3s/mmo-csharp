using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_STORMGUST2 — Magic splash water damage; high-rate variant.</summary>
public sealed class StormGust2 : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public StormGust2() : base(SkillIds.NPC_STORMGUST2) { }
    public StormGust2(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_STORMGUST2) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
