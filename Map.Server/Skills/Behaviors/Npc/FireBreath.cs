using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FIREBREATH — Magic hit; ratio +100*(lv-1); directional AoE TODO.</summary>
public sealed class FireBreath : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public FireBreath() : base(SkillIds.NPC_FIREBREATH) { }
    public FireBreath(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_FIREBREATH) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
