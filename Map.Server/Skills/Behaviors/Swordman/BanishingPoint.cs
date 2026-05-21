using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_BANISHINGPOINT — Royal Guard Banishing Point. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/banishingpoint.cpp</c>.
/// Ratio <c>+(-100 + 100*lv) + 70*Bash_lv</c>; +800 with SC_SPEAR_SCAR.
/// Hit bonus +5*lv. SM_BASH skill-tree query is TODO.
/// </summary>
public sealed class BanishingPoint : WeaponSkillImpl
{
    public BanishingPoint() : base(SkillIds.LG_BANISHINGPOINT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 100 * skillLevel);
        // TODO: + pc_checkskill(sd, SM_BASH) * 70 when skill tree is wired through.
        // TODO: + 800 if src has SC_SPEAR_SCAR.
        return ratio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + 5 * skillLevel);
}
