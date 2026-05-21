using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULFALCON — Falcon's Soul. Applies SC_SOULFALCON to target.</summary>
public sealed class FalconsSoul : StatusSkillImpl
{
    public FalconsSoul() : base(SkillIds.SP_SOULFALCON) { }
}
