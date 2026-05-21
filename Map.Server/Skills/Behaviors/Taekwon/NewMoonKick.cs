using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_NEWMOONKICK — Recursive splash; ratio +(600 + 100*lv).</summary>
public sealed class NewMoonKick : RecursiveDamageSplashSkillImpl
{
    public NewMoonKick() : base(SkillIds.SJ_NEWMOONKICK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600 + 100 * skillLevel;
}
