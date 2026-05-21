using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_GROUNDATTACK — Weapon hit; ratio +100*(lv-1).</summary>
public sealed class EarthAttributeAttack : WeaponSkillImpl
{
    public EarthAttributeAttack() : base(SkillIds.NPC_GROUNDATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
