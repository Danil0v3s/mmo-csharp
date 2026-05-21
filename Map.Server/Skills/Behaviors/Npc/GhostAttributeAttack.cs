using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_TELEKINESISATTACK — Weapon hit; ratio +100*(lv-1).</summary>
public sealed class GhostAttributeAttack : WeaponSkillImpl
{
    public GhostAttributeAttack() : base(SkillIds.NPC_TELEKINESISATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
