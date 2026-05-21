using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_PROMINENCEKICK — Recursive splash; ratio +(50 + 50*lv).</summary>
public sealed class ProminenceKick : RecursiveDamageSplashSkillImpl
{
    public ProminenceKick() : base(SkillIds.SJ_PROMINENCEKICK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 50 * skillLevel;
}
