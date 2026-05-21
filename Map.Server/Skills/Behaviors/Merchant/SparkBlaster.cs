using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SPARK_BLASTER — Meister Spark Blaster. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/sparkblaster.cpp</c>.
/// Ratio <c>+(-100 + 600 + 1400*lv) + 5*POW</c>.
/// </summary>
public sealed class SparkBlaster : RecursiveDamageSplashSkillImpl
{
    public SparkBlaster() : base(SkillIds.MT_SPARK_BLASTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 + 1400 * skillLevel) + 5 * src.Stats.Pow;
}
