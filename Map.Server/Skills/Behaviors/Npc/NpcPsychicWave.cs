using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PSYCHIC_WAVE — Splash magic damage. Ratio +50+50*lv (capped).</summary>
public sealed class NpcPsychicWave : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public NpcPsychicWave() : base(SkillIds.NPC_PSYCHIC_WAVE) { }
    public NpcPsychicWave(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_PSYCHIC_WAVE) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 50 * skillLevel;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
