using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_MUCHANAGE — Rapid Throw. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/rapidthrow.cpp</c>.
/// Recursive splash; hit rate gated by
/// <c>(100 - 1000/(dex+luk)*5) * (lv/2 + 5) / 10</c>.
/// </summary>
public sealed class RapidThrow : RecursiveDamageSplashSkillImpl
{
    public RapidThrow() : base(SkillIds.KO_MUCHANAGE) { }
}
