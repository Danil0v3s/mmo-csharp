using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_TRANSFORMATION — Mob transformation (changes appearance/stats).</summary>
public sealed class Transformation : StatusSkillImpl
{
    public Transformation() : base(SkillIds.NPC_TRANSFORMATION) { }
}
