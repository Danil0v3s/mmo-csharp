using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_EXPULSION — Self-buff (anti-undead push). StatusSkillImpl port.</summary>
public sealed class Expulsion : StatusSkillImpl { public Expulsion() : base(SkillIds.NPC_EXPULSION) { } }
