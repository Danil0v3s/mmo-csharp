using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WL_SOULEXPANSION — Warlock Soul Expansion. Splash magic; ratio +(-100+1000+200*lv) +INT.</summary>
public sealed class SoulExpansion : RecursiveDamageSplashSkillImpl
{
    public SoulExpansion() : base(SkillIds.WL_SOULEXPANSION) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + skillLevel * 200) + src.Stats.IntStat;
}
