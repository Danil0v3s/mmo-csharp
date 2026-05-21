using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_REBIRTH — Self-revive on next death.</summary>
public sealed class Rebirth : StatusSkillImpl
{
    public Rebirth() : base(SkillIds.NPC_REBIRTH) { }
}
