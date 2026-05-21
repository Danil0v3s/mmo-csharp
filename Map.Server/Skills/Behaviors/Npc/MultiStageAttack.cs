using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_COMBOATTACK — Weapon multi-stage hit; ratio +25*lv.</summary>
public sealed class MultiStageAttack : WeaponSkillImpl
{
    public MultiStageAttack() : base(SkillIds.NPC_COMBOATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 25 * skillLevel;
}
