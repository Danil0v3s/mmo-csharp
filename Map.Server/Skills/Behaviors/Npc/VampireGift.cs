using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_VAMPIRE_GIFT — Weapon hit; HP drain (% damage dealt).</summary>
public sealed class VampireGift : WeaponSkillImpl
{
    public VampireGift() : base(SkillIds.NPC_VAMPIRE_GIFT) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;
}
