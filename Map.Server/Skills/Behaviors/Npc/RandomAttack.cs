using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RANDOMATTACK — Mob random-damage weapon attack.</summary>
public sealed class RandomAttack : WeaponSkillImpl
{
    public RandomAttack() : base(SkillIds.NPC_RANDOMATTACK) { }
}
