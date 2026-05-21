using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_EARTHDRIVE — Royal Guard Earth Drive. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/earthdrive.cpp</c>.
/// Ratio <c>+(-100 + 380*lv) + STR + VIT</c>. SC_SHIELD_POWER bonus
/// scales with IG_SHIELD_MASTERY level — TODO. Splash cell wipe is TODO.
/// </summary>
public sealed class EarthDrive : RecursiveDamageSplashSkillImpl
{
    public EarthDrive() : base(SkillIds.LG_EARTHDRIVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 380 * skillLevel) + src.Stats.Str + src.Stats.Vit;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
