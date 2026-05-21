using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_COUNTERSLASH — Counter Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/counterslash.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 300 + 150*lv) + 2*Agi + 4*jobLv</c>.
/// 4th-class change_level_4th override is TODO.
/// </summary>
public sealed class CounterSlash : RecursiveDamageSplashSkillImpl
{
    public CounterSlash() : base(SkillIds.GC_COUNTERSLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 300 + 150 * skillLevel);
        ratio += src.Stats.Agi * 2;
        if (src is PlayerEntity p)
            ratio += p.JobLevel * 4;
        return ratio;
    }
}
