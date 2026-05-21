using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ANTIMAGIC — Mob self-buff (anti-magic). StatusSkillImpl port.</summary>
public sealed class AntiMagic : StatusSkillImpl { public AntiMagic() : base(SkillIds.NPC_ANTIMAGIC) { } }
