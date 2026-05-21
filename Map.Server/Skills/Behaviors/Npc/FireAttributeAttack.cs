using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FIREATTACK — Weapon hit; ratio +100*(lv-1).</summary>
public sealed class FireAttributeAttack : WeaponSkillImpl
{
    public FireAttributeAttack() : base(SkillIds.NPC_FIREATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
