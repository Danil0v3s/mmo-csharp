using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ALL_STAT_DOWN — Mob debuff. StatusSkillImpl port.</summary>
public sealed class DecreaseAllStats : StatusSkillImpl { public DecreaseAllStats() : base(SkillIds.NPC_ALL_STAT_DOWN) { } }
