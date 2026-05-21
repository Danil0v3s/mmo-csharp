using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CHANGEPOISON — Mob attribute change to Poison. StatusSkillImpl port.</summary>
public sealed class PoisonAttributeChange : StatusSkillImpl { public PoisonAttributeChange() : base(SkillIds.NPC_CHANGEPOISON) { } }
