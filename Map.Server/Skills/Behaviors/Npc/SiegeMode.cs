using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SIEGEMODE — Self siege buff (high DEF, low SPD).</summary>
public sealed class SiegeMode : StatusSkillImpl
{
    public SiegeMode() : base(SkillIds.NPC_SIEGEMODE) { }
}
