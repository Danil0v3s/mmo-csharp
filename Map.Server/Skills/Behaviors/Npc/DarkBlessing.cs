using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DARKBLESSING — (50 + 5*lv) % SC_COMA via SC start. Status start TODO.</summary>
public sealed class DarkBlessing : StatusSkillImpl { public DarkBlessing() : base(SkillIds.NPC_DARKBLESSING) { } }
