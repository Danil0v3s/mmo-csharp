using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_COUNTERSLASH — Counter Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/counterslash.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 300 + 150*lv)</c> with the
/// RE_LVL_DMOD(120) base-level scalar, plus <c>+Agi*2</c> and a
/// job-level term: 4th-class casters use the 3rd-class job-level at
/// promotion (rAthena <c>change_level_4th</c>); everyone else uses
/// the current JobLevel. Without a ChangeLevel4th field on
/// PlayerEntity we fall through to JobLevel for every PC.
/// </summary>
public sealed class CounterSlash : RecursiveDamageSplashSkillImpl
{
    /// <summary>rAthena <c>JOBL_FOURTH</c> — bit set on a PC's
    /// <c>class_</c> bitmask when they're a 4th-class job.</summary>
    private const int JOBL_FOURTH = 0x40000;

    public CounterSlash() : base(SkillIds.GC_COUNTERSLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 300 + 150 * skillLevel);
        ratio += src.Stats.Agi * 2;
        if (src is PlayerEntity pc)
        {
            // 4th-class branch reads change_level_4th (the 3rd-class
            // job-level captured at promotion); pending that field on
            // PlayerEntity we use the live JobLevel for both branches.
            ratio += pc.JobLevel * 4;
        }
        return ratio;
    }
}
