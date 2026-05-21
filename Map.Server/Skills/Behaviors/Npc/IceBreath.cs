using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ICEBREATH — Magic hit; ratio +100*(lv-1).</summary>
public sealed class IceBreath : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public IceBreath() : base(SkillIds.NPC_ICEBREATH) { }
    public IceBreath(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_ICEBREATH) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
