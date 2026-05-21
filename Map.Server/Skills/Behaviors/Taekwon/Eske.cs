using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SL_SKE — Eske. StatusSkillImpl port; chains SC_SMA on caster.</summary>
public sealed class Eske : StatusSkillImpl
{
    public Eske() : base(SkillIds.SL_SKE) { }
}
