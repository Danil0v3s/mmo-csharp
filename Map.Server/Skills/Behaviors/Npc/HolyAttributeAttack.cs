using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_HOLYATTACK — Weapon hit; ratio +100*(lv-1).</summary>
public sealed class HolyAttributeAttack : WeaponSkillImpl
{
    public HolyAttributeAttack() : base(SkillIds.NPC_HOLYATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
