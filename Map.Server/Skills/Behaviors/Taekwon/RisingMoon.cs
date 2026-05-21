using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_RISING_MOON — Recursive splash; applies SC_RISING_MOON and SC_MIDNIGHT_MOON to caster.</summary>
public sealed class RisingMoon : RecursiveDamageSplashSkillImpl
{
    public RisingMoon() : base(SkillIds.SKE_RISING_MOON) { }
}
