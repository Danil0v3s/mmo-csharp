using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RANDOMMOVE — Mob random walk command.</summary>
public sealed class RandomMove : StatusSkillImpl
{
    public RandomMove() : base(SkillIds.NPC_RANDOMMOVE) { }
}
