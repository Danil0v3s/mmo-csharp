using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_HALLUCINATION — Mob applies SC_HALLUCINATION. StatusSkillImpl port.</summary>
public sealed class Hallucination : StatusSkillImpl { public Hallucination() : base(SkillIds.NPC_HALLUCINATION) { } }
