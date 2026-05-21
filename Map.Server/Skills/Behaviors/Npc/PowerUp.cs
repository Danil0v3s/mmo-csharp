using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_POWERUP — Self ATK% +200 buff, broadcast no-damage.</summary>
public sealed class PowerUp : StatusSkillImpl
{
    public PowerUp() : base(SkillIds.NPC_POWERUP) { }
}
