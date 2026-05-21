using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_THUNDER_BREATH — Magic thunder breath; ratio +100*(lv-1).</summary>
public sealed class ThunderBreath : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public ThunderBreath() : base(SkillIds.NPC_THUNDERBREATH) { }
    public ThunderBreath(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_THUNDERBREATH) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
