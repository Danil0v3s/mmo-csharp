using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DEADLYCURSE — Mob skill that applies SC_DPOISON (StatusSkillImpl).</summary>
public sealed class DeadlyCurse : StatusSkillImpl { public DeadlyCurse() : base(SkillIds.NPC_DEADLYCURSE) { } }
