using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HAPPOKUNAI — Kunai Splash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunaisplash.cpp</c>.
/// Recursive splash; uses the inherited damage path.
/// </summary>
public sealed class KunaiSplash : RecursiveDamageSplashSkillImpl
{
    public KunaiSplash() : base(SkillIds.KO_HAPPOKUNAI) { }
}
