using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_ABYSS_DAGGER — Abyss Dagger. Manual port of
/// <c>rathena-fork/src/map/skills/thief/abyssdagger.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 350 + 1400*lv) + 5*pow</c>.
/// SC start happens via castendNoDamage hook (TODO).
/// </summary>
public sealed class AbyssDagger : RecursiveDamageSplashSkillImpl
{
    public AbyssDagger() : base(SkillIds.ABC_ABYSS_DAGGER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 + 1400 * skillLevel) + 5 * src.Stats.Pow;
}
