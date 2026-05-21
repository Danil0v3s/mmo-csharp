using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PIERCINGATT — Weapon hit; ratio -25 (75% base damage).</summary>
public sealed class PiercingAttack : WeaponSkillImpl
{
    public PiercingAttack() : base(SkillIds.NPC_PIERCINGATT) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio - 25;
}
