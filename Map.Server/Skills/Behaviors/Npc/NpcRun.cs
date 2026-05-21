using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RUN — Mob flees (random move at high speed).</summary>
public sealed class NpcRun : StatusSkillImpl
{
    public NpcRun() : base(SkillIds.NPC_RUN) { }
}
