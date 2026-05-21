using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_CURSEEXPLOSION — Recursive splash; ratio +(-100 + 400 + 100*lv) normally, +(-100 + 1200 + 300*lv) under SC_CURSE.</summary>
public sealed class CurseExplosion : RecursiveDamageSplashSkillImpl
{
    public CurseExplosion() : base(SkillIds.SP_CURSEEXPLOSION) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 100 * skillLevel);
}
