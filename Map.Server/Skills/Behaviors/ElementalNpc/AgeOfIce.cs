using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_AGE_OF_ICE — Elemental Age of Ice. Port of
/// <c>rathena-fork/src/map/skills/elemental/ageofice.cpp</c>.
/// Ratio <c>+(-100 + 3700)</c>; multiplied by (1 + masterLv/100).
/// The master-Lv lookup walks <c>ctx.Entities</c> via
/// <see cref="Entity.MasterId"/>; falls back to the elemental's own
/// level if the master isn't resolvable.
/// </summary>
public sealed class AgeOfIce : RecursiveDamageSplashSkillImpl
{
    public AgeOfIce() : base(SkillIds.EM_EL_AGE_OF_ICE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 3700);
        int masterLv = src.Level;
        if (src.MasterId is { } mid && ctx.Entities.Get(mid) is { } master)
            masterLv = master.Level;
        ratio += ratio * masterLv / 100;
        return ratio;
    }
}
