using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_IMPERIAL_CROSS — Imperial Guard Imperial Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/imperialcross.cpp</c>.
/// Ratio <c>+(-100 + 1650 + 1350*lv) + 5*POW</c>; +25 per
/// IG_SPEAR_SWORD_M; <c>+(100 + 300*lv)</c> with SC_SPEAR_SCAR.
/// Skill-tree + SC bonuses are TODO.
/// </summary>
public sealed class ImperialCross : WeaponSkillImpl
{
    public ImperialCross() : base(SkillIds.IG_IMPERIAL_CROSS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1650 + 1350 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
