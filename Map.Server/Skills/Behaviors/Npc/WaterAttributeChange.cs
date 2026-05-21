using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CHANGEWATER — Mob attribute change to Water. StatusSkillImpl port.</summary>
public sealed class WaterAttributeChange : StatusSkillImpl { public WaterAttributeChange() : base(SkillIds.NPC_CHANGEWATER) { } }
