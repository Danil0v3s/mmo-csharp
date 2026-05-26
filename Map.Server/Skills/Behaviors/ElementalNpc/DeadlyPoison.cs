using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_DEADLY_POISON — Elemental Deadly Poison. Port of
/// <c>rathena-fork/src/map/skills/elemental/deadlypoison.cpp</c>.
/// Ratio <c>+(-100 + 700)</c>, scaled by (1 + masterLv/100).
/// </summary>
public sealed class DeadlyPoison : RecursiveDamageSplashSkillImpl
{
    public DeadlyPoison() : base(SkillIds.EM_EL_DEADLY_POISON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 700);
        int masterLv = src.Level;
        if (src.MasterId is { } mid && ctx.Entities.Get(mid) is { } master)
            masterLv = master.Level;
        ratio += ratio * masterLv / 100;
        return ratio;
    }
}
