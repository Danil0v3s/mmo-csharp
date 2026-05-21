using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MAGICALATTACK — Grants SC_MAGICALATTACK self-buff. StatusSkillImpl port.</summary>
public sealed class DemonShockAttack : StatusSkillImpl { public DemonShockAttack() : base(SkillIds.NPC_MAGICALATTACK) { } }
