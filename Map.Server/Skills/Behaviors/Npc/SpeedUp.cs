using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SPEEDUP — Self movement speed buff.</summary>
public sealed class SpeedUp : StatusSkillImpl
{
    public SpeedUp() : base(SkillIds.NPC_SPEEDUP) { }
}
