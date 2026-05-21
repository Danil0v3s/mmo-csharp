using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_AGE_OF_ICE — Elemental Age of Ice. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/ageofice.cpp</c>.
/// Ratio <c>+(-100 + 3700)</c>; multiplied by (1 + masterLv/100) when
/// the elemental's master is known. Master-Lv lookup is TODO; we use
/// the caster's own Lv as a stand-in.
/// </summary>
public sealed class AgeOfIce : RecursiveDamageSplashSkillImpl
{
    public AgeOfIce() : base(SkillIds.EM_EL_AGE_OF_ICE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 3700);
        ratio += ratio * src.Level / 100;
        return ratio;
    }
}
