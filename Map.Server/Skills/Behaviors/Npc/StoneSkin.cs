using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_STONESKIN — Self DEF buff.</summary>
public sealed class StoneSkin : StatusSkillImpl
{
    public StoneSkin() : base(SkillIds.NPC_STONESKIN) { }
}
