using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SPLASHER — Venom Splasher. Manual port of
/// <c>rathena-fork/src/map/skills/thief/venomsplasher.cpp</c>.
/// Recursive splash; renewal ratio <c>+(-100 + 400 + 100*lv)</c>;
/// AS_POISONREACT partner bonus is TODO.
/// </summary>
public sealed class VenomSplasher : RecursiveDamageSplashSkillImpl
{
    public VenomSplasher() : base(SkillIds.AS_SPLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 100 * skillLevel);
}
