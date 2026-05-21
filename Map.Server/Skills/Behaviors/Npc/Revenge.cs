using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_REVENGE — Mob retaliation state (self buff increasing ATK on damage).</summary>
public sealed class Revenge : StatusSkillImpl
{
    public Revenge() : base(SkillIds.NPC_REVENGE) { }
}
