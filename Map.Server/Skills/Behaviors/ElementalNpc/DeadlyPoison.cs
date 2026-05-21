using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_DEADLY_POISON — Elemental Deadly Poison. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/deadlypoison.cpp</c>.
/// Ratio <c>+(-100 + 700)</c>, scaled by (1 + masterLv/100). Master-Lv
/// lookup is TODO; using the caster's Lv as a stand-in.
/// </summary>
public sealed class DeadlyPoison : RecursiveDamageSplashSkillImpl
{
    public DeadlyPoison() : base(SkillIds.EM_EL_DEADLY_POISON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 700);
        ratio += ratio * src.Level / 100;
        return ratio;
    }
}
