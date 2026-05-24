using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_BANISHINGPOINT — Royal Guard Banishing Point (skill.cpp:LG_BANISHINGPOINT).
/// Ratio <c>baseRatio + (-100 + 100*lv) + 70*pc_checkskill(SM_BASH)</c>;
/// +800 when the caster has <c>SC_SPEAR_SCAR</c> active. Hit rate
/// bonus <c>+5*lv</c>.
/// </summary>
public sealed class BanishingPoint : WeaponSkillImpl
{
    public BanishingPoint() : base(SkillIds.LG_BANISHINGPOINT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var bashLv = (src as PlayerEntity)?.LearnedSkills.GetValueOrDefault(SkillIds.SM_BASH) ?? 0;
        var ratio = baseRatio + (-100 + 100 * skillLevel) + 70 * bashLv;
        if (ctx.Sc?.Get(src, StatusType.SpearScar) != null) ratio += 800;
        return ratio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + 5 * skillLevel);
}
