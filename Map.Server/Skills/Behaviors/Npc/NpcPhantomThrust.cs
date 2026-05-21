using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PHANTOMTHRUST — Weapon-thrust attack; ratio +50*lv.</summary>
public sealed class NpcPhantomThrust : WeaponSkillImpl
{
    public NpcPhantomThrust() : base(SkillIds.NPC_PHANTOMTHRUST) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
