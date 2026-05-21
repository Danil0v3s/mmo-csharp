using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SLOWCAST — Target SC_SLOWCAST debuff.</summary>
public sealed class SlowCast : StatusSkillImpl
{
    public SlowCast() : base(SkillIds.NPC_SLOWCAST) { }
}
