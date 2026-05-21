using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_IMMUNE_PROPERTY — Self property-immunity buff.</summary>
public sealed class PropertyImmune : StatusSkillImpl
{
    public PropertyImmune() : base(SkillIds.NPC_IMMUNE_PROPERTY) { }
}
