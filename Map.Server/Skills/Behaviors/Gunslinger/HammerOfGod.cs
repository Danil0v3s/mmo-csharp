using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_HAMMER_OF_GOD — Rebellion Hammer of God. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/hammerofgod.cpp</c>.
/// Ratio <c>+(-100 + 100*lv) + 150*coins</c> (or 400*coins if target has
/// SC_C_MARKER from caster). Coin tracking is TODO; we use the
/// non-marker formula at a default 5 coins.
/// </summary>
public sealed class HammerOfGod : RecursiveDamageSplashSkillImpl
{
    public HammerOfGod() : base(SkillIds.RL_HAMMER_OF_GOD) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 * skillLevel) + 150 * 5;
}
