using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_EXORCISM_OF_MALICIOUS_SOUL — Recursive splash; ratio +(-100 + 150*lv) + spl. SoulMastery + soulball stack bonuses TODO.</summary>
public sealed class ExorcismOfMaliciousSoul : RecursiveDamageSplashSkillImpl
{
    public ExorcismOfMaliciousSoul() : base(SkillIds.SOA_EXORCISM_OF_MALICIOUS_SOUL) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 150 * skillLevel) + src.Stats.Spl;
}
