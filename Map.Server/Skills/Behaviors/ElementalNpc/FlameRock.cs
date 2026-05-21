using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_FLAMEROCK — Elemental Flame Rock. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/flamerock.cpp</c>.
/// Ratio <c>+(-100 + 2400)</c>, scaled by (1 + masterLv/100). Master-Lv
/// lookup is TODO; using the caster's Lv as a stand-in.
/// </summary>
public sealed class FlameRock : RecursiveDamageSplashSkillImpl
{
    public FlameRock() : base(SkillIds.EM_EL_FLAMEROCK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 2400);
        ratio += ratio * src.Level / 100;
        return ratio;
    }
}
