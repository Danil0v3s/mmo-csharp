using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SL_SKA — Eska. StatusSkillImpl port. Ends SC_SWOO if already up; otherwise applies SC_SKA.</summary>
public sealed class Eska : StatusSkillImpl
{
    public Eska() : base(SkillIds.SL_SKA) { }
}
