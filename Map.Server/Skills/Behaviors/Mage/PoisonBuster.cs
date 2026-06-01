using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_POISON_BUSTER — Sorcerer Poison Buster. Manual port of
/// <c>rathena-fork/src/map/skills/mage/poisonbuster.cpp</c>.
///
/// <para>Splash skill. Ratio: <c>+(-100 + 1000 + 300*lv) + INT</c>,
/// with +<c>200*lv</c> when SC_CLOUD_POISON is on the target and
/// +<c>job_level*5</c> with SC_CURSED_SOIL_OPTION on the caster.</para>
/// </summary>
public sealed class PoisonBuster : RecursiveDamageSplashSkillImpl
{
    public PoisonBuster() : base(SkillIds.SO_POISON_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 1000 + 300 * skillLevel) + src.Stats.IntStat;
        if (ctx.Sc?.Get(target, StatusType.CloudPoison) != null)
            ratio += 200 * skillLevel;
        if (ctx.Sc?.Get(src, StatusType.CursedSoilOption) != null && src is PlayerEntity pc)
            ratio += pc.JobLevel * 5;
        return ratio;
    }
}
