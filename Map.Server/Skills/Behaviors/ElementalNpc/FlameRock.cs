using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_FLAMEROCK — Elemental Flame Rock. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/flamerock.cpp</c>.
/// Ratio <c>+(-100 + 2400)</c>, scaled by (1 + masterLv/100).
/// </summary>
public sealed class FlameRock : RecursiveDamageSplashSkillImpl
{
    public FlameRock() : base(SkillIds.EM_EL_FLAMEROCK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 2400);
        int masterLv = src.Level;
        if (src.MasterId is { } mid && ctx.Entities.Get(mid) is { } master)
            masterLv = master.Level;
        ratio += ratio * masterLv / 100;
        return ratio;
    }
}
