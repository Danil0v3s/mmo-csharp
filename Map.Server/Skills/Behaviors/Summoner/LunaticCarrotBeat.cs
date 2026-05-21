using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_LUNATICCARROTBEAT — Summoner Lunatic Carrot Beat. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/lunaticcarrotbeat.cpp</c>.
/// Ratio <c>+(100 + 100*lv)</c>; <c>+STR</c> at BaseLv > 99.
/// </summary>
public sealed class LunaticCarrotBeat : RecursiveDamageSplashSkillImpl
{
    public LunaticCarrotBeat() : base(SkillIds.SU_LUNATICCARROTBEAT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 100 + 100 * skillLevel;
        if (src.Level > 99)
            ratio += src.Stats.Str;
        return ratio;
    }
}
